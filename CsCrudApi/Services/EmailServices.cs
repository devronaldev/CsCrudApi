using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using System.Net;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;
using SendGrid.Helpers.Mail.Model;

namespace CsCrudApi.Services
{
    public class EmailServices
    {
        private readonly string _apiKey;
        private static IConfiguration _configuration;
        public EmailServices()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _configuration = configurationBuilder;
            _apiKey = _configuration["SendGridKey"];
        }

        public async Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("A chave da API SendGrid não está configurada.");
            }

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("ronald.evangelista@aluno.ifsp.edu.br", "Conectando Saberes");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode != HttpStatusCode.Accepted && response.StatusCode != HttpStatusCode.OK)
            {
                // Aqui você pode logar ou tratar o erro da forma que preferir
                throw new Exception($"Erro ao enviar e-mail1: {response.StatusCode} - Metódo: SendEmailAsync");
            }
        }

        //E-MAILs DE VERIFICAÇÃO:
        public static async Task SendVerificationEmail(User user)
        {
            var token = TokenServices.GenerateToken(user); // Gerando o token para o usuário

            //TROCAR URL POR VARIÁVEL DE AMBIENTE
            var verificationLink = $"http://localhost:5042/api/UserAuth/verificar-email?token={token}";            
            var cancelationLink = $"http://localhost:5042/api/UserAuth/cancelar-cadastro?email={user.Email}";
            var plainTextContent = $"Por favor, clique no link para verificar seu e-mail: {verificationLink}. Caso você não tenha solicitado cadastro, clique nesse link para cancelar inscrição: {cancelationLink}";
            string htmlContent = GetHTMLContent("HTML/VerificationEmail.html");
            
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

        public static async Task ChangeEmailVerification(EmailVerification emailVerification, DateTime now)
        {
            var htmlContent = GetHTMLContent("HTML/VerificationNewEmail.html");
            string verificationLink = $"http://localhost:5042/api/User/trocar-email?token={emailVerification.VerificationToken}";
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
            var htmlContent = GetHTMLContent("HTML/ChangePasswordAdvice.html");

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
            var htmlContent = GetHTMLContent("HTML/ChangeEmailAdvice.html");
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


        private static string GetHTMLContent(string path)
        {
            string htmlContent = "";
            using (var arquivoHTML = File.OpenText(path))
            {
                htmlContent = arquivoHTML.ReadToEnd();
            }
            return htmlContent;
        }
    }
}