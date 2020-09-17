using System.IO;
using Xunit.Abstractions;
using System.Text;

namespace Tests
{
    public class Loggy : TextWriter
    {
        ITestOutputHelper _output;
        public Loggy(ITestOutputHelper output)
        {
            _output = output;
        }
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
        public override void WriteLine(string message)
        {
            _output.WriteLine(message);
        }
        public override void WriteLine(string format, params object[] args)
        {
            _output.WriteLine(format, args);
        }
    }

}


