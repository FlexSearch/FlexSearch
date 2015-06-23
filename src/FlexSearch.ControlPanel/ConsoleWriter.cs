using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FlexSearch.ControlPanel
{
    public class ConsoleWriter : TextWriter
    {
        private TextBox textbox;
        private Dispatcher dispatcher;
        public ConsoleWriter(TextBox textbox, Dispatcher dispatcher)
        {
            this.textbox = textbox;
            this.dispatcher = dispatcher;
        }

        public override void Write(char value)
        {
            this.dispatcher.Invoke(() => { textbox.Text += value; });
        }

        public override void Write(string value)
        {
            this.dispatcher.Invoke(() => { textbox.Text += value; });
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}
