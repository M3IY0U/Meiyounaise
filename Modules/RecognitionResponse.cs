using System.Collections.Generic;

namespace Meiyounaise.Modules
{
    public class Category
    {
        public string name { get; set; }
        public double score { get; set; }
    }

    public class Caption
    {
        public string text { get; set; }
        public double confidence { get; set; }
    }

    public class Description
    {
        public IList<string> tags { get; set; }
        public IList<Caption> captions { get; set; }
    }

    public class Metadata
    {
        public int width { get; set; }
        public int height { get; set; }
        public string format { get; set; }
    }

    public class Color
    {
        public string dominantColorForeground { get; set; }
        public string dominantColorBackground { get; set; }
        public IList<string> dominantColors { get; set; }
        public string accentColor { get; set; }
        public bool isBWImg { get; set; }
    }

    public class RecognitionResponse
    {
        public IList<Category> categories { get; set; }
        public Description description { get; set; }
        public string requestId { get; set; }
        public Metadata metadata { get; set; }
        public Color color { get; set; }
    }


}