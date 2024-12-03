using CsCrudApi.Models.UserRelated;
using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;
using CsCrudApi.Models.UserRelated.Request;
using DotNetEnv;

namespace CsCrudApi.Services
{
    public class EmailServices
    {
        private readonly string? _apiKey;
        private readonly string? _rootRoute;
        
        public EmailServices()
        {
            _apiKey = Environment.GetEnvironmentVariable("SEND_GRID_KEY");
            _rootRoute = Environment.GetEnvironmentVariable("ROOT_ROUTE");
        }

        //Email Sender
        public async Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("A chave da API SendGrid não está configurada.");
            }

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(Environment.GetEnvironmentVariable("EMAIL_SENDER"), "Conectando Saberes");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.OK)
            {
                // Aqui você pode logar ou tratar o erro da forma que preferir
                throw new Exception($"Erro ao enviar e-mail1: {response.Headers.ToString()} - Metódo: SendEmailAsync");
            }
        }

        //E-MAILs DE VERIFICAÇÃO:
        public static async Task SendVerificationEmail(User user)
        {
            var token = TokenServices.GenerateToken(user); // Gerando o token para o usuário
            string rootRoute = EmailServices.GetRootRoute();
            
            //TROCAR URL POR VARIÁVEL DE AMBIENTE
            var verificationLink = string.Format($"{rootRoute}api/UserAuth/verificar-email?token={token}");
            var cancelationLink = $"{rootRoute}api/UserAuth/cancelar-cadastro?email={user.Email}";
            var plainTextContent = $"Por favor, clique no link para verificar seu e-mail: {verificationLink}. Caso você não tenha solicitado cadastro, clique nesse link para cancelar inscrição: {cancelationLink}";
            string htmlContent = await GetHTMLContent("VerificationEmail");
            
            htmlContent = htmlContent.Replace("##NOME##", user.NmSocial);
            htmlContent = htmlContent.Replace("##LINK_VERIFICACAO##", verificationLink);
            htmlContent = htmlContent.Replace("##LINK_EXCLUIR_CADASTRO##", cancelationLink);

            try
            {
                var emailService = new EmailServices();
                await emailService.SendEmailAsync(user.Email, "Verificação de E-mail", plainTextContent, htmlContent);
            }
            catch (Exception ex)
            {
                // LOG O ERRO OU TRATE DE ACORDO
                Console.WriteLine($"Erro ao enviar e-mail2: {ex.Message} - Metódo: SendVerificationEmail");
            }
        }

        public static async Task ChangeEmailVerification(EmailVerification emailVerification)
        {
            string rootRoute = EmailServices.GetRootRoute();
            var htmlContent = await GetHTMLContent("VerificationNewEmail");
            string verificationLink = $"{rootRoute}api/User/trocar-email?token={emailVerification.VerificationToken}";
            string plainTextContent = $"Clique no link {verificationLink} para verificar o novo e-mail, caso não tenha sido você clique aqui para cancelar a requisição: FUTURO LINK" ;
            htmlContent = htmlContent.Replace("##LINK_VERIFICACAO##", verificationLink);

            try
            {
                var emailService = new EmailServices();
                await emailService.SendEmailAsync(emailVerification.NewEmail, "Verificação de novo E-mail", plainTextContent, htmlContent);
            }
            catch (Exception ex)
            {
                // LOG O ERRO OU TRATE DE ACORDO
                Console.WriteLine($"Erro ao enviar e-mail2: {ex.Message} - Metódo: ChangeEmailVerification");
            }
        }

        //IMPLEMENTAR PARA MODO "ESQUECEU A SENHA"
        /*
        public static async Task SendPasswordChangeEmail (LoginDTO user)
        {
            LoginDTO loginDTO = new LoginDTO() { Email = user.Email, Password = user.Password};
        }
        */

        //E-MAILs DE AVISO - IMPLEMENTAR CANCELAMENTO DE ALTERAÇÕES:
        public static async Task ChangePasswordAdvice (User user, DateTime now)
        {
            var htmlContent = await GetHTMLContent("ChangePasswordAdvice");

            htmlContent = htmlContent.Replace("##NOME##", user.NmSocial);
            htmlContent = htmlContent.Replace("##DIA_ALTERACAO##", $"{now.Day}/{now.Month}/{now.Year}");
            htmlContent = htmlContent.Replace("##HORA_ALTERACAO##", $"{now.Hour}:{now.Minute}:{now.Second}");
            string plainTextContent = $"Olá {user.NmSocial}, sua senha foi alterada no dia {now.Day}/{now.Month}/{now.Year} às {now.Hour}:{now.Minute}:{now.Second}, caso não tenha sido você clique nesse link: FUTURO LINK";

            try
            {
                var emailService = new EmailServices();
                await emailService.SendEmailAsync(user.Email, "Alteração de Senha", plainTextContent, htmlContent);
            }
            catch (Exception ex)
            {
                // LOG O ERRO OU TRATE DE ACORDO
                Console.WriteLine($"Erro ao enviar e-mail: {ex.Message} - Metódo: ChangePasswordAdvice");
            }
        }

        public static async Task ChangeEmailAdvice (User user, DateTime now, string newEMail)
        {
            var htmlContent = await GetHTMLContent("ChangeEmailAdvice");
            string plainTextContent = $"Olá {user.NmSocial}, houve uma alteração no seu e-mail no dia {now.Day} às {now.TimeOfDay}, caso não tenha sido você clique nesse link: FUTURO LINK";

            htmlContent = htmlContent.Replace("##NOME##", user.NmSocial);
            htmlContent = htmlContent.Replace("##NOVO_EMAIL##", newEMail);
            htmlContent = htmlContent.Replace("##DIA_ALTERACAO##", $"{now.Day}/{now.Month}/{now.Year}");
            htmlContent = htmlContent.Replace("##HORA_ALTERACAO##", $"{now.Hour}:{now.Minute}:{now.Second}");

            try
            {
                var emailService = new EmailServices();
                await emailService.SendEmailAsync(user.Email, "Alteração de Email", plainTextContent, htmlContent);
            }
            catch (Exception ex) { 
                // LOG O ERRO OU TRATE DE ACORDO
                Console.WriteLine($"Erro ao enviar e-mail: {ex.Message} - Metódo: ChangeEmailAdvice");
            }
        }


        private static async Task<string> GetHTMLContent(string templateName)
        {
            // Define a raiz do projeto
            var rootPath = AppContext.BaseDirectory;

            // Combina a raiz do projeto com o caminho do HTML
            var filePath = Path.Combine(rootPath, "HTML", $"{templateName}.html");

            // Lê o conteúdo do arquivo HTML
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            else
            {
                throw new FileNotFoundException($"Template '{templateName}' não encontrado em '{filePath}'.");
            }
        }

        public static string GetRootRoute()
        {
            EmailServices services = new();
            return services._rootRoute;
            //ISSO PODE DAR MUITO ERRADO; CONSERTAR;
        }
    }
}