using System.Collections.Generic;

namespace Hercules.Scripting
{
    public class SyntaxError
    {
        public int Position { get; set; }
    }

    public class SyntaxValidator
    {
        public List<SyntaxError> Errors { get; private set; }

        public SyntaxValidator()
        {
            Errors = new List<SyntaxError>();
        }
    }
}
