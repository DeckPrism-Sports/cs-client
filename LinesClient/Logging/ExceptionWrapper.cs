using System;
using System.Text;

namespace DPSports.Exceptions
{
    /// <summary>
    /// Wraps Exceptions for better display during logging.
    /// </summary>
    public class ExceptionWrapper : Exception
    {
        private readonly static string _indent = "    ";
        private Exception _exception;

        /// <summary>
        /// True if this wrapper has an exception wrapped.
        /// </summary>
        public bool HasValue { get { return _exception != null; } }     

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exception">Exception to be wrapped</param>
        public ExceptionWrapper(Exception exception)
        {
            _exception = exception;
        }

        private static void ComposeExceptionDescription(StringBuilder b, Exception ex, string indent)
        {
            b.AppendLine(indent + ex.GetType().FullName);
            b.AppendLine(indent + "Message: " + ex.Message);

            if(ex.TargetSite != null)
                b.AppendLine(indent + "TargetSite: " + ex.TargetSite);

            if(!string.IsNullOrWhiteSpace(ex.Source))
                b.AppendLine(indent + "Source: " + ex.Source);

            var childIndent = indent + _indent;

            if(ex.StackTrace?.Length > 0)
            {
                b.AppendLine(indent + "StackTrace: ");
                var lines = ex.StackTrace.Split('\n');
                var lineCount = lines.Length;
                for(int i = 0; i < lineCount; i++)
                {
                    b.AppendLine(childIndent + lines[i].Trim());
                }
            }

            if(ex.InnerException != null)
            {
                b.AppendLine();
                b.AppendLine(childIndent + "-----------------Inner Exception-----------------");
                ComposeExceptionDescription(b, ex.InnerException, childIndent);               
                b.AppendLine();
            }
        }

        /// <summary>
        /// Overriden to display more detailed information.
        /// </summary>
        /// <returns>Formatted exception information</returns>
        public override string ToString()
        {
            if(_exception == null)
                return "";

            var b = new StringBuilder(1000);
            b.AppendLine();

            ComposeExceptionDescription(b, _exception, _indent);

            return b.ToString();
        }
    }
}
