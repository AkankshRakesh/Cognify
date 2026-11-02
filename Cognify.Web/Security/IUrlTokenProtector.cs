namespace Cognify.Web.Security
{
    public interface IUrlTokenProtector
    {
        string ProtectExam(int id);
        bool TryUnprotectExam(string? token, out int id);
        string ProtectAttempt(int id);
        bool TryUnprotectAttempt(string? token, out int id);
        string ProtectQuestion(int id);
        bool TryUnprotectQuestion(string? token, out int id);
        string ProtectQuestionSet(int id);
        bool TryUnprotectQuestionSet(string? token, out int id);
    }
}
