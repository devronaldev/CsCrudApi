using CsCrudApi.Models.User;
using CsCrudApi.Services;
using System.Net;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;

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
            var plainTextContent = $"Por favor, clique no link para verificar seu e-mail: {verificationLink}";
            var htmlContent = $"<strong>Por favor, clique no link para verificar seu e-mail:</strong> <a href='{verificationLink}'>Verificar E-mail</a>";
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
    }
}