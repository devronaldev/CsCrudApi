using CsCrudApi.Models.User;
using CsCrudApi.Services;
using System.Net;
using System.Net.Mail;

namespace Services
{
    public class EmailServices
    {
        //VERIFICAR SMTP E SERVIÇOS VIA GOOGLE CLOUD.
        public static async Task SendVerificationEmail(User user)
        {
            var token = TokenServices.GenerateToken(user); // Gerando o token para o usuário
            var verificationLink = $"https://seusite.com/verificar-email?token={token}";
            var message = new MailMessage();
            message.To.Add(user.Email);
            message.Subject = "Verifique seu e-mail";
            message.Body = $"Clique no link para verificar seu e-mail: {verificationLink}";
            // Configure SMTP e envie o e-mail

            using (var smtpClient = new SmtpClient("smtp.seusite.com"))
            {
                smtpClient.Port = 587; // ou 465, dependendo do seu provedor
                smtpClient.Credentials = new NetworkCredential("seu_email@seusite.com", "sua_senha");
                smtpClient.EnableSsl = true; // Ative SSL se necessário

                try
                {
                    await smtpClient.SendMailAsync(message);
                }
                catch (Exception ex)
                {
                    // Log o erro ou trate de acordo
                    Console.WriteLine($"Erro ao enviar e-mail: {ex.Message}");
                }
            }
        }
    }
}