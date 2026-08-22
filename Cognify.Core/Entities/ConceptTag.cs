namespace Cognify.Core.Entities
{
    public class ConceptTag
    {
        public int ConceptTagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
