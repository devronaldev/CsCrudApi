using CsCrudApi.Models.UserRelated;
using CsCrudApi.Services;
using System.Net;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;
using SendGrid.Helpers.Mail.Model;

namespace Services
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

        //IMPLEMENTAR PARA MODO "ESQUECEU A SENHA"
        /*
        public static async Task SendPasswordChangeEmail (LoginDTO user)
        {
            LoginDTO loginDTO = new LoginDTO() { Email = user.Email, Password = user.Password};
        }
        */

        //IMPLEMENTAR EMAIL PARA AVISO DE SENHA ALTERADA!
        public static async Task ChangePasswordAdvice (User user, DateTime now)
        {
            var htmlContent = GetHTMLContent("HTML/ChangePasswordAdvice.html");

            htmlContent = htmlContent.Replace("##NOME##", user.NmSocial);
            htmlContent = htmlContent.Replace("##DIA_ALTERACAO##", now.Date.ToString());
            htmlContent = htmlContent.Replace("##HORA_ALTERACAO##", now.TimeOfDay.ToString());
            var plainTextContent = $"Olá {user.NmSocial}, sua senha foi alterada no dia {now.Date.ToString()} às {now.TimeOfDay.ToString()}, caso não tenha sido você clique nesse link: FUTURO LINK";

            try
            {
                var emailService = new EmailServices();
                await emailService.SendEmailAsync(user.Email, "Alteração de Senha", plainTextContent, htmlContent);
            }
            catch (Exception ex)
            {
                // LOG O ERRO OU TRATE DE ACORDO
                Console.WriteLine($"Erro ao enviar e-mail2: {ex.Message} - Metódo: SendVerificationEmail");
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