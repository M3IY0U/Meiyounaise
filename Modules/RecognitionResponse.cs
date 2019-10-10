using System.Collections.Generic;

namespace Meiyounaise.Modules
{
    public class Category
    {
        public string Name { get; set; }
        public double Score { get; set; }
    }

    public class Caption
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
    }

    public class Description
    {
        public IList<string> Tags { get; set; }
        public IList<Caption> Captions { get; set; }
    }

    public class Metadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; }
    }

    public class Color
    {
        public string DominantColorForeground { get; set; }
        public string DominantColorBackground { get; set; }
        public IList<string> DominantColors { get; set; }
        public string AccentColor { get; set; }
        public bool IsBwImg { get; set; }
    }

    public class RecognitionResponse
    {
        public IList<Category> Categories { get; set; }
        public Description Description { get; set; }
        public string RequestId { get; set; }
        public Metadata Metadata { get; set; }
        public Color Color { get; set; }
    }
}