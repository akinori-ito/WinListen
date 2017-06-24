using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;
//using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace WinListen
{
    class Recognizer
    {
        SpeechRecognitionEngine sr;
        Int32 port = 10500;
        static TcpClient client = null;
        static StreamWriter sout = null;

        // initialize with the dictation grammar
        public Recognizer(bool listenmode)
        {
            prepareDictationEngine("en-US");
            initHandler(listenmode);
        }

        // initalize with the word grammar
        public Recognizer(bool listenmode, String[] wordlist)
        {
            prepareWordRecognitionEngine("en-US",wordlist);
            initHandler(listenmode);
        }

        void initHandler(bool listenmode)
        {
            if (listenmode)
                sr.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sr_SpeechRecognized);
            else
                sr.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sr_debug);
        }
        void sr_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (client == null)
                return;
            RecognitionResult r = e.Result;
            if (r.Text.Length == 0)
            {
                sout.WriteLine("<RECOGFAIL/>");
                return;
            }
            Console.WriteLine(r.Text);
            sout.WriteLine("<RECOGOUT>");
            double score = Math.Log(r.Confidence);
            sout.WriteLine("  <SHYPO RANK=\"1\" SCORE=\"" + score.ToString() + "\">");
            foreach (RecognizedWordUnit w in r.Words)
            {
                String word = w.Text;
                String phone = w.Pronunciation;
                sout.WriteLine("    <WHYPO WORD=\"" + word + "\" PHONE=\"" + phone + "\" CM=\"" + r.Confidence + "\">");
            }
            sout.WriteLine("  </SHYPO>");
            sout.WriteLine("</RECOGOUT>");
            sout.WriteLine(".");
            try
            {
                sout.Flush();
            }
            catch (IOException ex)
            {
                client = null;
                sout = null;
            }
        }

        void sr_debug(object sender, SpeechRecognizedEventArgs e)
        {
            RecognitionResult r = e.Result;
            Console.WriteLine(r.Text);
        }

        void prepareDictationEngine(String culture)
        {
            System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo(culture);
            sr = new SpeechRecognitionEngine(cultureInfo);
            sr.SetInputToDefaultAudioDevice();
            sr.LoadGrammar(new DictationGrammar());
        }

        void prepareWordRecognitionEngine(String culture, String[] wordlist)
        {
            System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo(culture);
            sr = new SpeechRecognitionEngine(cultureInfo);
            sr.SetInputToDefaultAudioDevice();
            Choices colors = new Choices();
            colors.Add(wordlist);

            GrammarBuilder gb = new GrammarBuilder();
            gb.Culture = cultureInfo;
            gb.Append(colors);

            //Create the Grammar instance.
            Grammar g = new Grammar(gb);
            //Grammar g = new DictationGrammar();
            sr.LoadGrammar(g);
            //sr.LoadGrammar(new DictationGrammar());
        }

        public void listen()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            for (;;)
            {
                try
                {
                    client = listener.AcceptTcpClient();
                    Console.WriteLine("Client accepted");
                    sout = new StreamWriter(client.GetStream());
                    for (;;)
                    {
                        if (client == null || !client.Connected)
                            throw new IOException();
                        sr.Recognize();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("Client disconnected");
                }
            }

        }

        public void debug()
        {
            for (;;)
            {
                Console.WriteLine("Level="+sr.AudioLevel);
                sr.Recognize();
            }
        }
    }

    class Program
    {
        static String[] readWordList(String filename)
        {
            List<String> lines = new List<String>();
            TextReader tr = File.OpenText(filename);
            for (;;)
            {
                String line = tr.ReadLine();
                if (line == null) break;
                if (line.Length == 0) continue;
                lines.Add(line);
            }
            tr.Close();
            return lines.ToArray();
        }
        static void Main(string[] args)
        {
            bool listenmode = true;
            String[] wordlist = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-debug")
                {
                    listenmode = false;
                }
                else if (args[i] == "-sentence")
                {
                    wordlist = readWordList(args[++i]);
                }
                else
                    throw new Exception("Unknown option " + args[i]);
            }
            Recognizer recog;
            if (wordlist == null)
                recog = new Recognizer(listenmode);
            else
                recog = new Recognizer(listenmode, wordlist);
            if (listenmode)
                recog.listen();
            else
                recog.debug();
        }
    }
}
