namespace Coracle.Samples.ClientHandling
{
    public class Note
    {
        public string UniqueHeader { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return $"Header:{UniqueHeader} Text:{Text}";
        }
    }
}
