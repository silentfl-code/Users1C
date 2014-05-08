using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using System.IO;
//using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Data.Odbc;
using System.Web;
using System.Reflection;
using System.ServiceProcess;

namespace Users1C
{
    public partial class Service1 : ServiceBase
    {
        struct Params
        {
            internal int Port;
        }

        [SerializableAttribute]
        public struct Node
        {
            public string ComputerName;
            public string ApplicationName;
            public DateTime ConnectionStarted;
            public int SessionNumber;
            public int ConnectionNumber;
            public string UserFullName;
        }

        const string connectionString = @"Srvr=""server1c""; Ref=""basename""; Pwd=password; Usr=user";
        public static object connection;
        public static Type oType;
        static private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        static private Thread _thread;
        public static Node[] nodes = new Node[0];
        public static Timer timer;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
#if NEEDLOG
            log("Start" + DateTime.Now.ToString());
#endif
            init();
            RefreshUserList(null);
#if NEEDLOG
            log("Users: " + nodes.Length.ToString());
#endif
            timer = new Timer(RefreshUserList, new object[] { }, 0, 15000);

            Params p = new Params();
            p.Port = 8080;
            _thread = new Thread(MyServer);
            _thread.Start(p);
#if NEEDLOG
            log("Start main thread: OK");
#endif
        }

        protected override void OnStop()
        {
            _shutdownEvent.Set();
            if (!_thread.Join(3000))
            {
                _thread.Abort();
            };
#if NEEDLOG
            log("Stop main thread: OK");
#endif
        }

        static void init()
        {
            oType = Type.GetTypeFromProgID("V82.COMConnector");
            if (oType != null)
            {
                object V8 = Activator.CreateInstance(oType);
                try
                {
                    connection = oType.InvokeMember("Connect", BindingFlags.Public | BindingFlags.InvokeMethod, null, V8, new object[] { connectionString });
                }
                catch (Exception e)
                {
                    Marshal.ReleaseComObject(V8);
#if NEEDLOG
                    log(e.Message);
                    log(e.InnerException.ToString());
#endif
                    return;
                }
            }
            else 
            {
#if NEEDLOG
                log("Error V82 COM-object create!");
#endif
            }
        }
#if NEEDLOG
        static public void log(string message)
        { 
            try
            {
                if (!EventLog.SourceExists("Users1C"))
                {
                    EventLog.CreateEventSource("Users1C", "Users1C");
                }
                eventLog1.Source = "Users1C";
                eventLog1.WriteEntry(message);
            }
            catch
            {
            }
        }
#endif

        static void RefreshUserList(Object StateInfo)
        {
            if (connection == null)
            {
                timer.Dispose();
            }
            else {
                object usersList = oType.InvokeMember("ПолучитьСоединенияИнформационнойБазы", BindingFlags.Public | BindingFlags.InvokeMethod, null, connection, new object[] { });  
                object usersCount = oType.InvokeMember("Количество", BindingFlags.Public | BindingFlags.InvokeMethod, null, usersList, new object[] { });
                Node [] tmp = new Node[(int)usersCount];
                for (int i = 0; i < (int)usersCount; i++)
                {
                    object Str = oType.InvokeMember("Get", BindingFlags.Public | BindingFlags.InvokeMethod, null, usersList, new object[] { i });
                    object user = oType.InvokeMember("Пользователь", BindingFlags.Public | BindingFlags.GetProperty, null, Str, new object[] { });
                    tmp[i].UserFullName = (string)oType.InvokeMember("ПолноеИмя", BindingFlags.Public | BindingFlags.GetProperty, null, user, new object[] { });
                    tmp[i].ApplicationName = (string)oType.InvokeMember("ИмяПриложения", BindingFlags.Public | BindingFlags.GetProperty, null, Str, new object[] { });
                    tmp[i].ComputerName = (string)oType.InvokeMember("ИмяКомпьютера", BindingFlags.Public | BindingFlags.GetProperty, null, Str, new object[] { });
                    tmp[i].ConnectionNumber = (int)oType.InvokeMember("НомерСоединения", BindingFlags.Public | BindingFlags.GetProperty, null, Str, new object[] { });
                    tmp[i].ConnectionStarted = (DateTime)oType.InvokeMember("НачалоСоединения", BindingFlags.Public | BindingFlags.GetProperty, null, Str, new object[] { });
                    tmp[i].SessionNumber = (int)oType.InvokeMember("НомерСоединения", BindingFlags.Public | BindingFlags.GetProperty, null, Str, new object[] { });
                    Marshal.ReleaseComObject(user);
                    Marshal.ReleaseComObject(Str);
                }
                lock (nodes)
                {
                    nodes = tmp;
                }
                //Marshal.ReleaseComObject(usersCount);
                Marshal.ReleaseComObject(usersList);
            }
        }

        static public string GetXml(string FilePath)
        {
            string res = "";
            StringWriter stringWriter = new Utf8StringWriter(); 
            XmlWriter xmlWriter = XmlTextWriter.Create(stringWriter);
            XmlSerializer serializer = new XmlSerializer(typeof(Node[]));
            Node [] tmp;
            switch (FilePath)
            { 
                case "www/top.xml":
                    lock (nodes)
                    {
                        tmp = (Node[])(nodes.Clone());
                    }
                    serializer.Serialize(xmlWriter, tmp);
                    res = stringWriter.ToString();
                    break;
                default:
                    break;
            }
            return res;
        }
    
        static private void MyServer(object ob)
        {
            Params p = (Params)ob;
            int MaxThreadsCount = Environment.ProcessorCount * 4;         // Установим максимальное количество рабочих потоков
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);   // Установим минимальное количество рабочих потоков
            ThreadPool.SetMinThreads(2, 2);                               // Создаем "слушателя" для указанного порта
            TcpListener Listener = new TcpListener(IPAddress.Any, p.Port);
            Listener.Start(); // Запускаем его

            while (!_shutdownEvent.WaitOne(0))
            {
                if (!Listener.Pending()) {
                    Thread.Sleep(50);
                    continue;
                };
                // Принимаем новых клиентов
                ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), Listener.AcceptTcpClient());
            }
        }

        static void ClientThread(Object StateInfo)
        {
            new Client((TcpClient)StateInfo);
        }

        /*
        static void AddLog(string message)
        {
            lock (TMP.Program.syncRoot)
            {
                using (var file = File.Open("users1c.log", FileMode.Append))
                using (var stream = new StreamWriter(file))
                {
                    stream.WriteLine(message);
                }
            }
        }
         * */
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }

    class Client
    {
        // Конструктор класса. Ему нужно передавать принятого клиента от TcpListener
        public Client(TcpClient Client)
        {
            // Объявим строку, в которой будет хранится запрос клиента
            string Request = "";
            // Буфер для хранения принятых от клиента данных
            byte[] Buffer = new byte[1024];
            // Переменная для хранения количества байт, принятых от клиента
            int Count;
            // Читаем из потока клиента до тех пор, пока от него поступают данные
            while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                // Преобразуем эти данные в строку и добавим ее к переменной Request
                Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                // Запрос должен обрываться последовательностью \r\n\r\n
                // Либо обрываем прием данных сами, если длина строки Request превышает 4 килобайта
                // Нам не нужно получать данные из POST-запроса (и т. п.), а обычный запрос
                // по идее не должен быть больше 4 килобайт
                if (Request.IndexOf("\r\n\r\n") >= 0 || Request.Length > 4096)
                {
                    break;
                }
            }

#if NEEDLOG            
            //записываем полученные от клиента запросы
            Service1.log(Request);
#endif
            // Парсим строку запроса с использованием регулярных выражений
            // При этом отсекаем все переменные GET-запроса
            Match ReqMatch = Regex.Match(Request, @"^\w+\s+([^\s\?]+)[^\s]*\s+HTTP/.*|");
            
            // Если запрос не удался
            if (ReqMatch == Match.Empty)
            {
                // Передаем клиенту ошибку 400 - неверный запрос
                SendError(Client, 400);
                return;
            }

            // Получаем строку запроса
            string RequestUri = ReqMatch.Groups[1].Value;
#if NEEDLOG
            Service1.log("client: " + ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString() + "\tRequestUri: " + RequestUri);
#endif
            if (RequestUri == "") RequestUri = "/";

            // Приводим ее к изначальному виду, преобразуя экранированные символы
            // Например, "%20" -> " "
            RequestUri = Uri.UnescapeDataString(RequestUri);

            // Если в строке содержится двоеточие, передадим ошибку 400
            // Это нужно для защиты от URL типа http://example.com/../../file.txt
            if (RequestUri.IndexOf("..") >= 0)
            {
#if NEEDLOG
                Service1.log("404");
#endif
                SendError(Client, 400);
                return;
            }

            // Если строка запроса оканчивается на "/", то добавим к ней index.html
            if (RequestUri.EndsWith("/"))
            {
                RequestUri += "index.html";
            }

            // Получаем расширение файла из строки запроса
            string Extension = RequestUri.Substring(RequestUri.LastIndexOf('.'));

            string FilePath = "www"+RequestUri;
//            if (Extension != ".xml") FilePath = Environment.CurrentDirectory + "/www" + RequestUri;
//            else FilePath = "www" + RequestUri;

            // Если в папке www не существует данного файла, посылаем ошибку 404
            //исключение - динамически генерируемые xml-файлы
            if ((Extension != ".xml") && (!File.Exists(FilePath)))
            {
#if NEEDLOG
                Service1.log("404 (*.xml)");
#endif
                SendError(Client, 404);
                return;
            }

            // Тип содержимого
            string ContentType = "";

            // Пытаемся определить тип содержимого по расширению файла
            switch (Extension)
            {
                case ".htm":
                case ".html":
                    ContentType = "text/html";
                    break;
                case ".css":
                    //ContentType = "text/stylesheet";
                    ContentType = "text/css";
                    break;
                case ".js":
                    ContentType = "text/javascript";
                    break;
                case ".jpg":
                    ContentType = "image/jpeg";
                    break;
                case ".jpeg":
                case ".png":
                case ".gif":
                    ContentType = "image/" + Extension.Substring(1);
                    break;
                case ".xml":
                    ContentType = "text/xml";
                    break;
                default:
                    if (Extension.Length > 1)
                    {
                        ContentType = "application/" + Extension.Substring(1);
                    }
                    else
                    {
                        ContentType = "application/unknown";
                    }
                    break;
            }
            if (ContentType == "text/xml")
            {
                // Посылаем заголовки
                string xml = Service1.GetXml(FilePath);

                StreamWriter fout = new StreamWriter("output.txt", true);
                fout.Write(xml);
                fout.Write("====================================================");
                fout.Close();
                /*
                string Headers = "HTTP/1.1 200 OK\nContent-Type: " + ContentType + " charset=utf-8" + "\nContent-Length: ";
                Headers = Headers + (xml.Length + Headers.Length + 2).ToString() + "\n\n";
                //byte[] HeadersBuffer = Encoding.ASCII.GetBytes(Headers);
                byte[] HeadersBuffer = Encoding.UTF8.GetBytes(Headers);
                Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);
                 //* */
                
                byte[] Buff = Encoding.UTF8.GetBytes(xml);
                Client.GetStream().Write(Buff, 0, Buff.Length);
                Client.Close();
            }
            else
            {
                // Открываем файл, страхуясь на случай ошибки
                FileStream FS;
                try
                {
                    FS = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (Exception)
                {
                    // Если случилась ошибка, посылаем клиенту ошибку 500
                    SendError(Client, 500);
                    return;
                }

                // Посылаем заголовки
                string Headers = "HTTP/1.1 200 OK\nContent-Type: " + ContentType + "\nContent-Length: " + FS.Length + "\n\n";
                byte[] HeadersBuffer = Encoding.ASCII.GetBytes(Headers);
                Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);

                // Пока не достигнут конец файла
                while (FS.Position < FS.Length)
                {
                    // Читаем данные из файла
                    Count = FS.Read(Buffer, 0, Buffer.Length);
                    // И передаем их клиенту
                    Client.GetStream().Write(Buffer, 0, Count);
                }

                // Закроем файл и соединение
                FS.Close();
                Client.Close();
            }
        }

        private void SendError(TcpClient Client, int Code)
        {
            // Получаем строку вида "200 OK"
            // HttpStatusCode хранит в себе все статус-коды HTTP/1.1
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            // Код простой HTML-странички
            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
            // Необходимые заголовки: ответ сервера, тип и длина содержимого. После двух пустых строк - само содержимое
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            // Приведем строку к виду массива байт
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            // Отправим его клиенту
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            // Закроем соединение
            Client.Close();
        } 
    }
}
