using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Cognify.Web.Security
{
    public sealed class UrlTokenProtector : IUrlTokenProtector
    {
        private readonly IDataProtectionProvider _provider;

        public UrlTokenProtector(IDataProtectionProvider provider)
        {
            _provider = provider;
        }

        public string ProtectExam(int id) => Protect(id, "exam");
        public bool TryUnprotectExam(string? token, out int id) => TryUnprotect(token, "exam", out id);
        public string ProtectAttempt(int id) => Protect(id, "attempt");
        public bool TryUnprotectAttempt(string? token, out int id) => TryUnprotect(token, "attempt", out id);
        public string ProtectQuestion(int id) => Protect(id, "question");
        public bool TryUnprotectQuestion(string? token, out int id) => TryUnprotect(token, "question", out id);
        public string ProtectQuestionSet(int id) => Protect(id, "question-set");
        public bool TryUnprotectQuestionSet(string? token, out int id) => TryUnprotect(token, "question-set", out id);

        private string Protect(int id, string category)
        {
            return _provider.CreateProtector($"Cognify.UrlIdentifiers.v1.{category}")
                .Protect(id.ToString(CultureInfo.InvariantCulture));
        }

        private bool TryUnprotect(string? token, string category, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                var value = _provider.CreateProtector($"Cognify.UrlIdentifiers.v1.{category}").Unprotect(token);
                return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
    }
}
