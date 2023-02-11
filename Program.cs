using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using static Google.Cloud.Firestore.V1.StructuredQuery.Types;
using System.Collections;
//using System.Web.Script.Serialization;

namespace TescoTest // Note: actual namespace depends on the project name.
{
	internal class Program
    {
        static string path = AppDomain.CurrentDomain.BaseDirectory + @"cufe12345-tescowebsite-firebase-adminsdk-1fdk2-d3bf6e6cb3.json";
        static FirestoreDb db;

        public static HttpClient client;
        static bool runServer = true;
        public static HttpListener listener;
        public static string[] url = {"http://*:8080/","https://*:8443/"};
        static void Main(string[] args)
        {
			Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
			 db = FirestoreDb.Create("cufe12345-tescowebsite");
            //Testing();
            //var task = TestIfOnlineAsync();
            //int i = 0;
            //while (i <= 10) { 
            //     FetchProducts(1); 
            //      i++;
            //      Thread.Sleep(2000);
            //  }
            if (args.Length != 0)
            {
                if (args[0] == "-O" || args[0] == "-A" || args[0] == "-T")
                {
                    Menu(args);
                }
            }
            else
            {
                StartServer();
                // Console.Write(RemoveProduct(1, 264625916, 1));
                // Console.WriteLine(AddToOrder(264625916, 1, 1,"test","pizza"));
                //  Console.WriteLine(AddToCart(4));
                //Console.WriteLine(PutRequest(264625916, "pizza"));
            }
                while (true)
                {
                    int a = 0;
                }
            
        }
        async public static void Menu(string[] args)
        {
			if (args[0] == "-O")
			{
				Console.WriteLine(await AddOrder(args[1]));
			}
			else if (args[0] == "-A")
			{
				Console.WriteLine(await AddToCart(args[1]));
			}
            else if (args[0] == "-T")
            {
                Console.WriteLine(await Total(args[1]));
            }
		}
        async public static Task<int> Total(string orderId)
        {
            List<object> products = await GetProducts(orderId);
            double total = 0;
            Dictionary<string, double> people = new Dictionary<string, double>();
            if (products[0] == "null")
            {
                return -1;
            }
            try
            {
                List<object> addedProducts = new List<object>();
                for (int i = 0; i < products.Count; i++)
                {
                    object product = (object)products[i];
                    double price = 0;
                    string owner = "";
                    foreach (KeyValuePair<string, object> fields in (Dictionary<string, object>)product)
                    {
                        if(fields.Key == "price")
                        {
                            price = double.Parse(fields.Value.ToString());
                        }
                        if(fields.Key == "owner")
                        {
                            owner = fields.Value.ToString();
                        }
                    }
                    try
                    {
                        if(price == 0)
                        {
                            return -1;
                        }
                        people[owner] += price;
                        total += price;
                    }
                    catch (Exception)
                    {
                        people.Add(owner, price);
                        total += price;
                    }
                }
            }
            catch (Exception)
            {
                return -1;
            }
            foreach(string person in people.Keys)
            {
                Console.WriteLine(person+" total: " + people[person].ToString());
            }
            Console.WriteLine("Total is: " + total.ToString());
            return 0;

        }

        async public static void Testing()
        {
			DocumentReference docRef = db.Collection("users").Document();
			Dictionary<string, object> user = new Dictionary<string, object>
            {
			    { "username", "Ada" },
			    { "password", "Lovelace" },
			    { "orderId","" }
            };
			await docRef.SetAsync(user);
		}
        public static void TestServer()
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://20.68.14.122:8080");
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/json";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"data\": \"test\"";

                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
        }

        public static void StartServer()
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            foreach (string address in url)
            {
                listener.Prefixes.Add(address);
            }
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);
            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
        public static async Task HandleIncomingConnections()
        {
            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse response = ctx.Response;
                
                Console.WriteLine("RECIEVED");
                Console.WriteLine(req.HttpMethod);
                if (req.HttpMethod == "OPTIONS")
                {
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                    response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                    response.AddHeader("Access-Control-Max-Age", "1728000");
                    response.Close();
                }
                else if(req.HttpMethod == "POST")
                {
                    response.AppendHeader("Access-Control-Allow-Origin", "*");
                    Console.WriteLine("{\"Data\": \"WORKED\"}");
                    byte[] data = null;
                    using (System.IO.Stream body = req.InputStream) // here we have data
                    {
                        using (var reader = new System.IO.StreamReader(body, req.ContentEncoding))
                        {
                            string requestString = reader.ReadToEnd();
                            
                            bool val = false;
                            bool startReading = false;
                            string temp = "";
                            List<string> values = new List<string>();
                            string previousChar = "";
                            foreach(char c in requestString)
                            {
                                if(c.ToString() == ":" && previousChar !="s")
                                {
                                    val = true;
                                    startReading = false;
                                    temp = "";
                                }
                                else if (val)
                                {
                                    if(c.ToString() == "\"" && !startReading)
                                    {
                                        startReading = true;
                                    }
                                    else if (startReading)
                                    {
                                        if(c.ToString() == "\"")
                                        {
                                            val = false;
                                            values.Add(temp);

                                        }
                                        temp += c.ToString();
                                    }
                                }
                                previousChar = c.ToString();
                            }
                            Console.WriteLine("Type: " + values[0]);
                            if (values[0] == "SEARCH")
                            {
                                if (values[1] != " ")
                                {
                                    List<List<string>> postResults = PostRequest(values[1]);
                                    if (postResults != null)
                                    {
                                        string generatedResponse = "[";
                                        bool first = true;
                                        int index = 0;
                                        for (int i = 0; i < postResults[0].Count; i++)
                                        {
                                            if (!first)
                                            {
                                                generatedResponse += ",";

                                            }
                                            else
                                            {
                                                first = false;
                                            }
                                            float price = float.Parse(postResults[1][i]);
                                            string finalPrice = price.ToString();
                                            bool hasDot = false;
                                            int count = 0;
                                            foreach (char c in finalPrice)
                                            {
                                                if (c.ToString() == ".")
                                                {
                                                    hasDot = true;
                                                }
                                                else if (hasDot)
                                                {
                                                    count++;
                                                }
                                            }
                                            if (!hasDot)
                                            {
                                                finalPrice += ".00";
                                            }
                                            else if (count != 2)
                                            {
                                                finalPrice += "0";
                                            }
                                            if (postResults[4][i] != "null" && postResults[4][i] != "null2")
                                            {
                                                generatedResponse += "{\"price\" : \"" + finalPrice +" / "+postResults[4][i]+" ClubCard\",";
                                                
                                            }
                                            else if (postResults[4][i] == "null2")
                                            {
                                                generatedResponse += "{\"price\" : \"" + finalPrice + " / ERROR ClubCard\",";
                                            }
                                            else
                                            {
                                                generatedResponse += "{\"price\" : \"" + finalPrice + "\",";
                                            }
                                            generatedResponse += " \"title\" : \"" + postResults[0][i] + "\",";
                                            generatedResponse += " \"id\" : \"" + postResults[3][i] + "\",";
                                            generatedResponse += " \"image\" : \"" + postResults[2][i] + "\"}";
                                        }
                                        generatedResponse += "]";
                                        data = Encoding.UTF8.GetBytes(generatedResponse);
                                    }
                                }

                            }
                            else if (values[0] == "CREATE")
                            {
                                string username = "";
                                string password = "";
                                bool first = true;
                                foreach (char c in values[1])
                                {
                                    if (first)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            first = false;
                                        }
                                        else
                                        {
                                            username += c.ToString();
                                        }
                                    }
                                    else
                                    {
                                        password += c.ToString();
                                    }
                                }
                                int value = await CreateAccount(username, password);
                                data = Encoding.UTF8.GetBytes("{ \"Result\": \"" + value.ToString() + "\"}");
                            }
                            else if (values[0] == "LOGIN")
                            {
                                Console.WriteLine("Content: " + values[1]);
                                string username = "";
                                string password = "";
                                bool first = true;
                                foreach (char c in values[1])
                                {
                                    if (first)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            first = false;
                                        }
                                        else
                                        {
                                            username += c.ToString();
                                        }
                                    }
                                    else
                                    {
                                        password += c.ToString();
                                    }
                                }
                                List<object> value = await Login(username, password);
                                Console.WriteLine("RESPONSE: "+value[0].ToString());
                                data = Encoding.UTF8.GetBytes("[{ \"Result\": \"" + value[0].ToString() + "\"}, {\"Name\": \"" + value[1].ToString() + "\"}]");
                            }
                            else if (values[0] == "FETCH_ORDERS")
                            {
                                List<List<string>> orders = await FetchOrders();
                                string generatedResponse = "";
                                if (orders[0][0] != "ERROR")
                                {
                                    List<string> order = orders[0];
                                    generatedResponse += "[";
                                    generatedResponse += "{\"id\" : \"" + order[0] + "\",";
                                    generatedResponse += " \"title\" : \"Order\",";
                                    generatedResponse += " \"image\" : \"https://upload.wikimedia.org/wikipedia/commons/6/6d/Good_Food_Display_-_NCI_Visuals_Online.jpg\",";
                                    generatedResponse += " \"price\" :\"" + order[2] + "\",";
                                    generatedResponse += " \"date\" : \"" + order[1] + "\"}";
                                    for (int i = 1; i < orders.Count; i++)
                                    {
                                        order = orders[i];
                                        generatedResponse += ",{\"id\" : \"" + order[0] + "\",";
                                        generatedResponse += " \"title\" : \"Order\",";
                                        generatedResponse += " \"image\" : \"https://upload.wikimedia.org/wikipedia/commons/6/6d/Good_Food_Display_-_NCI_Visuals_Online.jpg\",";
                                        generatedResponse += " \"price\" :\"" + order[2] + "\",";
                                        generatedResponse += " \"date\" : \"" + order[1] + "\"}";
                                    }
                                    generatedResponse += "]";
                                    data = Encoding.UTF8.GetBytes(generatedResponse);
                                }
                            }
                            else if (values[0] == "ADD")
                            {
                                string word = "";
                                int id = -1;
                                float cost = -1;
                                string orderId = "-1";
                                string userId = "-1";
                                string query = "null";
                                string name = "null";
                                string image = "null";
                                bool first = true;
                                bool second = true;
                                bool third = true;
                                bool fourth = true;
                                bool five = true;
                                bool six = true;
                                foreach (char c in values[1])
                                {
                                    if (first)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            first = false;
                                            id = Convert.ToInt32(word);
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else if (second)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            second = false;
                                            if (word.Contains("ClubCard"))
                                            {
                                                string final = "";
                                                bool slash = false;
                                                bool space = false;
                                                string initialPrice = "";
                                                int indexPrice = 0;
                                                foreach(char ch in word)
                                                {
                                                    if(indexPrice < 4)
                                                    {
                                                        initialPrice += ch;
                                                        indexPrice++;
                                                    }
                                                    if(ch.ToString() == "/")
                                                    {
                                                        slash = true;
                                                    }
                                                    if (slash)
                                                    {
                                                        if (!space)
                                                        {
                                                            if (ch.ToString() == " ")
                                                            {
                                                                space = true;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if(ch.ToString() == " ")
                                                            {
                                                                break;
                                                            }
                                                            final += ch.ToString();
                                                        }
                                                    }
                                                }
                                                if (final != "ERROR")
                                                {
                                                    word = final;
                                                }
                                                else
                                                {
                                                    word = initialPrice;
                                                }
                                            }
                                                cost = float.Parse(word);
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else if (third)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            third = false;
                                            try
                                            {
                                                orderId = word;
                                            }
                                            catch (Exception)
                                            {
                                                orderId = "-1";
                                            }
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else if (fourth)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            fourth = false;
                                            userId = word.ToString();
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else if (five)
                                    {
                                        if(c.ToString() == ",")
                                        {
                                            five = false;
                                            query = word.ToString();
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else if (six)
                                    {
                                        if(c.ToString() == ",")
                                        {
                                            six = false;
                                            name = word.ToString();
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else
                                    {

                                        word += c.ToString();

                                    }

                                }
                                image = word;
                                word = "";
                                if (id == -1 || cost == -1 || orderId == "-1" || userId == "-1" || query == "null" || name == "null" || image == "null")
                                {

                                }
                                else
                                {
                                    int result = await AddToOrderV2(id, cost, orderId, userId, query,name,image);

                                    string generatedResponse = "";
                                    generatedResponse += "{\"result\" : " + result + "}";
                                    data = Encoding.UTF8.GetBytes(generatedResponse);


                                }
                            }
                            else if (values[0] == "BASKET")
                            {
                                if (values[1] != null && values[1] != "No order")
                                {
                                    List<List<string>> postResults = await FetchProducts(values[1]);
                                    if (postResults != null && postResults[0][0] != "-1")
                                    {
                                        string generatedResponse = "[";
                                        bool first = true;
                                        for (int i = 0; i < postResults[0].Count; i++)
                                        {
                                            if (first)
                                            {
                                                first = false;
                                            }
                                            else
                                            {
                                                generatedResponse += ",";
                                            }
                                            float price = float.Parse(postResults[1][i]);
                                            string finalPrice = price.ToString();
                                            bool hasDot = false;
                                            int count = 0;
                                            foreach (char c in finalPrice)
                                            {
                                                if (c.ToString() == ".")
                                                {
                                                    hasDot = true;
                                                }
                                                else if (hasDot)
                                                {
                                                    count++;
                                                }
                                            }
                                            if (!hasDot)
                                            {
                                                finalPrice += ".00";
                                            }
                                            else if (count != 2)
                                            {
                                                finalPrice += "0";
                                            }
                                            generatedResponse += "{\"price\" : \"" + finalPrice + "\",";
                                            generatedResponse += " \"title\" : \"" + postResults[0][i] +"\'s "+ postResults[3][i] + "\",";
                                            generatedResponse += " \"id\" : \"" + postResults[2][i] + "\",";
                                            generatedResponse += " \"image\" : \"" + postResults[4][i] + "\"}";
                                           
                                        }
                                        generatedResponse += "]";
                                        Console.WriteLine("WWW: "+generatedResponse);
                                        data = Encoding.UTF8.GetBytes(generatedResponse);
                                    }
                                }
                            }
                            else if (values[0] == "REMOVE")
                            {
                                string requestV = values[1];
                                float price = -1;
                                int id = -1;
                                string orderId = "-1";
                                string userId = "-1";
                                bool first = true;
                                bool second = true;
                                bool third = true;
                                string word = "";
                                foreach(char c in requestV)
                                {
                                    if (first)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            first = false;
                                            price = float.Parse(word);
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else if (second)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            second = false;
                                            id = Convert.ToInt32(word);
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else if (third)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            third = false;
                                            orderId = word;
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else
                                    {


                                        word += c.ToString();

                                    }
                                }
                                userId= (word);
                                if (price != -1 && id != -1 && orderId != "-1" && userId != "-1")
                                {
                                    int result = await RemoveProduct(price, id, orderId,userId);
                                    string generatedResponse = "";
                                    generatedResponse += "{\"result\" : " + result + "}";
                                    data = Encoding.UTF8.GetBytes(generatedResponse);
                                }
                            }
                            else if (values[0] == "PRICE")
                            {
                                string orderId = "-1";
                                try
                                {
                                    orderId = values[1];
                                }
                                catch (Exception)
                                {
                                    
                                }
                                if (orderId != "-1")
                                {
                                    string price = await GetPrice(orderId);
                                    bool hasDot = false;
                                    int count = 0;

                                    foreach (char c in price)
                                    {
                                        if (c.ToString() == ".")
                                        {
                                            hasDot = true;
                                        }
                                        else if (hasDot)
                                        {
                                            count++;
                                        }
                                    }
                                    if (!hasDot)
                                    {
                                        price += ".00";
                                    }
                                    else if (count != 2)
                                    {
                                        price += "0";
                                    }
                                    data = Encoding.UTF8.GetBytes("{ \"Result\": \"" + price + "\"}");
                                }

                            }
                            else if (values[0] == "USER_PRICE")
                            {
                                string orderId = "-1";
                                string user = "-1";
                                bool first = false;
                                string word = "";
                                foreach (char c in values[1])
                                {
                                    if (!first)
                                    {
                                        if (c.ToString() == ",")
                                        {
                                            first = true;
                                            user = word;
                                            word = "";
                                        }
                                        else
                                        {
                                            word += c.ToString();
                                        }
                                    }
                                    else
                                    {
                                        word += c.ToString();
                                    }
                                }
                                if (word != "No order")
                                {
                                    orderId = word;
                                }
                                if(orderId != "-1" && user != "-1")
                                {
                                    double price = await GetUserPrice(user, orderId);
                                    price = Math.Round(price, 2);
                                    data = Encoding.UTF8.GetBytes("{ \"Result\": \"" + price.ToString() + "\"}");
                                }
                            }
                        }
                    }
                    if (data == null)
                    {
                        data = Encoding.UTF8.GetBytes(String.Format("{{\"Data\": \"Error\"}}"));
                    }

                    //application/json
                    response.ContentType = "application/json";
                    response.ContentEncoding = Encoding.UTF8;
                    response.ContentLength64 = data.LongLength;
                    await response.OutputStream.WriteAsync(data, 0, data.Length);
                    response.Close();
                }
            }
        }
        public static async Task<int> CreateAccount(string username,string password)
        {
            bool result = await SQLCheckAccount(username);
            if (!result)
            {
                return SQLCreateAccount(username, password);
            }
            else
            {
                return -1;
            }
        }
        public static List<List<string>>PostRequest(string itemName)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://www.tesco.com/groceries/en-GB/resources");
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers["authority"] = "www.tesco.com";
            httpWebRequest.Accept = "application/json";
            httpWebRequest.Headers["accept-language"] = "en-GB,en-US;q=0.9,en;q=0.8";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Headers["cookie"] = "null; consumer=default; trkid=ed990236-5fb4-4166-ae7e-9ead9a6801b8; atrc=7018fe14-25aa-4b69-afb1-deec4dad0e2a; DCO=sdc; h-e=ec5a7f09a46b825f424c83ecad5ee4ff4ef3f09b188a7cb7f6719ff50f07a0b2; ighs-sess=eyJwYXNzcG9ydCI6eyJ1c2VyIjp7ImlkIjpudWxsfX0sImFuYWx5dGljc1Nlc3Npb25JZCI6IjgxZjI3MDA2OTU4NWJkOGNkOGYyOGU2OTM4YzFhZGYwIiwic3RvcmVJZCI6IjMwNjAifQ==; ighs-sess.sig=Ang8WGGdUz_LePlhZeuElYV3-Ww; null; itemsPerPage=48; atrc=7018fe14-25aa-4b69-afb1-deec4dad0e2a; _csrf=pVlJtEsVGpbMKV9V9TTVPGe7; UUID=52f2e6b4-3907-46b2-a67d-a302333784fe; CID=109635388; cookiePreferences=%7B%22experience%22%3Atrue%2C%22advertising%22%3Atrue%7D; AMCVS_E4860C0F53CE56C40A490D45%40AdobeOrg=1; s_vi=[CS]v1|31A6372D46EB7BED-600018EDAB62401D[CE]; s_ecid=MCMID%7C73815809893675098823618191616222616966; _gcl_au=1.1.935610389.1665953370; _4c_mc_=bb336543-3d61-4324-93b6-d9db09c3b08b; s_cc=true; _mibhv=anon-1665953371398-7165848800_4481; HYF=1; optimizelyEndUserId=oeu1666466686604r0.7714935891564776; s_nr=1669483533534-Repeat; ADRUM=s=1669483533553&r=https%3A%2F%2Fwww.tesco.com%2Faccount%2Flogin%2Fen-GB%3F-44598983; OAuth.RefreshToken=fb711379-5f67-41e7-9ee2-c205ab069c58; OAuth.TokensExpiryTime=%7B%22AccessToken%22%3A1669487132261%2C%22RefreshToken%22%3A1669487132261%7D; akavpau_OneAccount=1669483833~id=d5cbffd5f2dd6176c88cdb99388fc32c; bm_sz=4D5BCB279465EB5902FA55357B034CD1~YAAQEzZ6XP5toYqEAQAAXmdFzRFsoCS9xn7ahOSYejr3ureaDaSUZAJTOcTTsvBMl+y5SsQ1Nd36qL/+9qGKB7TQywg6eojobD8swJlyAmmDsypJXd8+PeWip/ivdpvQnAxS+2s0oreeilXvM4P5fXZMbkAM/toP4pjTk2yjgf/+K1886TMrxtn7CVmW/zq0bNiDKxJOvlAsms+Gn4CqRYmRSVEW7v00vVItXXJLfv7egWBCsmPzuqL0hZITaofBVKtgMGgRBLhog8Y7OUyL5c1YrV7QOisnM385WO0OvmUBaA==~4473649~4605251; ak_bmsc=D31B590726A0968ACD1EED04DE4DAFB3~000000000000000000000000000000~YAAQEzZ6XMBwoYqEAQAAYHtFzREjo6UFYaT7HZ0kdbAjOo13heDCfn5xuaSEVQPP3fN72LK/vQR3UtJiaZybvEb3kh1fiS0H9laU0pITXbZxgzQxjEGcM4Wu5QDMnmhWX0m0vLYl0Z0uxL1pqkTSvHwdJpkNW9hB1RWOxWVuKfDTUJbGizGzuMCSkAYXi4PAdKYpLc5gjksNmqK3XBRTUiBAO/vrWhztjc5rCTqddtt/U5Mt9ADfsbceyvQ155hs3fMxlUULesDSWj4JhbuEYosdCqvdcx66mvycWtQRaMvcdE4GBNPpdxBEjgmICYVKJ+rnqoCxkJeeBKA2e0ccvvBOOOodW5CFGKTbvgFgauLwB/Z91yIJgztqU/FiFQJV8g8AWTlyKOwMEFh0Qoob/UpLav6qPDEPfUwaLjo5QWKa3yJGSt1tfF11IpNYYWmpMUygI0quHpAbIIKuSlwvh/TIcLo6s2uk+2epidNfDPes42jOjFHPmfd8; AMCV_E4860C0F53CE56C40A490D45%40AdobeOrg=1585540135%7CMCIDTS%7C19328%7CMCMID%7C73815809893675098823618191616222616966%7CMCAAMLH-1670495998%7C6%7CMCAAMB-1670495998%7CRKhpRz8krg2tLO6pguXWp5olkAcUniQYPHaMWWgdJ3xzPWQmdj0y%7CMCOPTOUT-1669898398s%7CNONE%7CMCAID%7C31A6372D46EB7BED-600018EDAB62401D%7CvVersion%7C4.4.0; _gid=GA1.2.587739995.1669891200; AKA_A2=A; _abck=0534C241549C06CC77A180D147E2BE3B~0~YAAQRI9lX4LorcaEAQAAHDOfzQiPFzkoEby7+MtiXgDFIrN1yKccKSpwnn4iAPNoKQ0ezR3MPnGdeBduURrSb7askR0VFZNbi157667IGguMAbY1Du6aF1TguMnyaOJK4w7RhOtq2aj9UMo4CBqiVJreNfzUb6nu4bOslFo7Ku/iXABGWYkmY7z6y9+ha1/ZooccCiLtIshbL6IZYn3QLzVAUUTtsxAPfSfA7J+jdsfLE9LGmjTx6T5qKlp+aKl54KqxQPDCB6urbR1CATguSj1TExp62t66KtO77lm4PwID+2bKT1XbBqhc2M8Zj/SOELiyrIqU/T0aMglpYRPNjMbuHK4daESQUNT2lFQTgf1knKTOvvcWsWC88Gtb39GBEd6/w3yiKcLOdCkmwP9xzRxTmUjyOFQ=~-1~-1~-1; s_gpv_pn=search%20results; _ga=GA1.1.1578246787.1665953370; _ga_33B19D36CY=GS1.1.1669897076.13.1.1669897076.60.0.0; akavpau_tesco_groceries=1669897377~id=91d99346ff44dc3ad16c1bf917ef2911; _gat_ukLego=1; _uetsid=81955ce0716411ed9e8f83914aa69698; _uetvid=084b35004d9411ed804de5721711bbe8; bm_sv=7DB00FD9DDBC91BFA9E59F5291699CDE~YAAQRI9lX/DqrcaEAQAAxUSfzRH+A8EfCr/omD1wok0LQCjTdjyfzpdxl3oHXusMUQTg/ASvcvaE0pc1XVjBOhSK9ve4STb6gTbtYagdesc7LM40mF1BY7zXXc3CzwHtQf/eTOqipoUMgHcb8Nr2foDlxJs4ZWYwbtcAiqNWRIN10pIPwKtlVyZZeQNv4BWTdqEbpdjZJP+qzkWyNbnRfGXyH8kfg510D1HNJWc/T5OvA/GONCiNhNgo7MXcZQdL~1; da_sid=2A23C0C18E3DAE974903AA13B09BE7542A|4|0|4; da_lid=90FC84D19A73EA10B0A5BB99F5B6BD09F6|0|0|0; da_intState=";
            httpWebRequest.Headers["newrelic"] = "eyJ2IjpbMCwxXSwiZCI6eyJ0eSI6IkJyb3dzZXIiLCJhYyI6IjM1MTI5NTQiLCJhcCI6IjExMzQyNDYxMjUiLCJpZCI6IjRjZDgzMjAzMTNmNmQ1NzUiLCJ0ciI6ImYyZDgwNDJkMDNiN2IyZjFiYjZkYTQ4OGEwZjkyMGE0IiwidGkiOjE2Njk4OTcwODMzMzEsInRrIjoiMzI5NjIzNSJ9fQ==";
            httpWebRequest.Headers["origin"] = "https://www.tesco.com";
            httpWebRequest.Referer = "https://www.tesco.com/groceries/en-GB/search?query="+itemName;
            httpWebRequest.Headers["sec-ch-ua"] = "\"Google Chrome\";v=\"107\", \"Chromium\";v=\"107\", \"Not = A ? Brand\";v=\"24\"";
            httpWebRequest.Headers["sec-ch-ua-mobile"] = "?0";
            httpWebRequest.Headers["sec-ch-ua-platform"] = "\"Windows\"";
            httpWebRequest.Headers["sec-fetch-dest"] = "empty";
            httpWebRequest.Headers["sec-fetch-mode"] = "cors";
            httpWebRequest.Headers["sec-fetch-site"] = "same-origin";
            httpWebRequest.Headers["sentry-trace"] = "9c226dbbd0044c00aba44f06c0af31bc-aa13a57fcdd309f4-0";
            httpWebRequest.Headers["traceparent"] = "00-f2d8042d03b7b2f1bb6da488a0f920a4-4cd8320313f6d575-01";
            httpWebRequest.Headers["tracestate"] = "3296235@nr=0-1-3512954-1134246125-4cd8320313f6d575----1669897083331";
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36";
            httpWebRequest.Headers["x-csrf-token"] = "avGSQPH1-Ov_r1JF93eQUymIKE0iC7eoLR5M";
            httpWebRequest.Headers["x-queueit-ajaxpageurl"] = "true";
            httpWebRequest.Headers["x-requested-with"] = "XMLHttpRequest";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.Headers["Accept-Encoding"] = "gzip, deflate, br";
            httpWebRequest.Headers["x-requested-with"] = "XMLHttpRequest";
            httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"resources\":[{\"type\":\"appState\",\"params\":{}," +
                    "\"hash\":\"8608229003782371\"}," +
                    "{\"type\":\"experiments\",\"params\":{}," +
                    "\"hash\":\"3586087262245984\"}," +
                    "{\"type\":\"trolleyContents\"," +
                    "\"params\":{}," +
                    "\"hash\":\"2574718136506441\"}," +
                    "{\"type\":\"search\",\"params\":{\"query\":{\"query\":\"" + itemName + "\"}}," +
                    "\"hash\":\"5849940266468826\"}],\"sharedParams\":{\"query\":{\"query\":\""+itemName+"\"}}," +
                    "\"requiresAuthentication\":false}";

                streamWriter.Write(json);
            }
            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    // Console.WriteLine(result);
                    return ExtractDataV2(result);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static List<List<string>> ExtractData(string data)
        {
            bool titleB = false;
            bool priceB = false;
            bool imgUrlB = false;
            bool idB = false;
            bool promotionsB = false;
            string word = "";
            List<string> items = new List<string>();
            List<string> prices = new List<string>();
            List<string> imgUrls = new List<string>();
            List<string> ids = new List<string>();
            List<bool> promotionalPrice = new List<bool>();
            List<string> clubPrices = new List<string>();
            bool firstSpeech = true;
            bool products = false;
            foreach (char c in data){
                if (titleB)
                {
                    if (c.ToString() == "\"" && !firstSpeech)
                    {
                        items.Add(word);
                        firstSpeech = true;
                        word = "";
                        titleB = false;
                    }
                    else if (c.ToString() == "\"" && firstSpeech)
                    {
                        firstSpeech = false;
                    }
                    if (!firstSpeech)
                    {
                        word += c.ToString();
                    }
                }
                else if (imgUrlB)
                {
                    if (c.ToString() == "\"" && !firstSpeech)
                    {
                        imgUrls.Add(word);
                        firstSpeech = true;
                        word = "";
                        imgUrlB = false;
                    }
                    else if (c.ToString() == "\"" && firstSpeech)
                    {
                        firstSpeech = false;
                    }
                    if (!firstSpeech)
                    {
                        word += c.ToString();
                    }
                }
                else if (priceB)
                {
                    if (c.ToString() == "," && !firstSpeech)
                    {
                        float test;
                        try
                        {
                            test = float.Parse(word);
                        }
                        catch (Exception)
                        {
                            test = -1;
                        }
                        if (test != -1)
                        {
                            prices.Add(word);
                        }
                        firstSpeech = true;
                        word = "";
                        priceB = false;

                    }
                    else if (c.ToString() == " " && firstSpeech)
                    {
                        firstSpeech = false;
                    }
                    if (!firstSpeech)
                    {
                        if (c.ToString() != " ")
                        {
                            word += c.ToString();
                        }
                    }
                }
                else if (idB)
                {
                    if (c.ToString() == "\"" && !firstSpeech)
                    {
                        ids.Add(word);
                        firstSpeech = true;
                        word = "";
                        idB = false;
                    }
                    else if (c.ToString() == "\"" && firstSpeech)
                    {
                        firstSpeech = false;
                    }
                    if (!firstSpeech)
                    {
                        word += c.ToString();
                    }
                }
                else if (promotionsB)
                {
                    if(c.ToString() != "]")
                    {
                        word += c.ToString();
                    }
                    else
                    {
                        if(word != ": []")
                        {
                            string word2 = "";
                            bool offerTextB = false;
                            bool first = false;
                            foreach(char letter in word)
                            {
                                if(letter.ToString() == ",")
                                {
                                    word2 = "";
                                }
                                else if (offerTextB)
                                {
                                    if (!first)
                                    {
                                        if(letter.ToString() == "\"")
                                        {
                                            first = true;
                                        }
                                    }
                                    else
                                    {
                                        if(letter.ToString() == " ")
                                        {
                                            clubPrices.Add(word2);
                                            break;
                                        }
                                        else
                                        {
                                            word2 += letter.ToString();
                                        }
                                    }
                                }
                                else if (letter.ToString() == "\"")
                                {
                                    if(word2 == "offerText")
                                    {
                                        offerTextB = true;
                                        first = false;
                                    }
                                    else
                                    {
                                        word2 = "";
                                    }
                                }
                                else
                                {
                                    word2 += letter.ToString();
                                }
                            }
                            promotionsB = false;
                            promotionalPrice.Add(true);
                        }
                        else
                        {
                            word = "";
                            promotionsB = false;
                            promotionalPrice.Add(false);
                            
                        }
                    }
                }
                else if(c.ToString() != "\"")
                {
                    word += c.ToString();
                }
                else
                {
                    if(word == "title")
                    {
                        titleB = true;
                        word = "";
                    }
                    else if(word == "defaultImageUrl")
                    {
                        imgUrlB = true;
                        word = "";
                    }
                    else if(word == "price")
                    {
                        priceB = true;
                        word = "";
                    }
                    else if(word == "id" && products)
                    {
                        idB = true;
                        word = "";
                    }
                    else if(word == "product")
                    {
                        products = true;
                    }
                    else if(word == "promotions")
                    {
                        promotionsB = true;
                        word = "";
                    }
                    else
                    {
                        word = "";
                    }
                }
            }
            List<string> finalIds = new List<string>();
            foreach (string id in ids)
            {
                string finalId = "";
                foreach(char c in id)
                {
                    if (c.ToString() != "\"")
                    {
                        finalId += c;
                    }
                }
                try
                {
                    int test = Convert.ToInt32(finalId);
                }
                catch (Exception)
                {
                    continue;
                }
                finalIds.Add(finalId);
              // Console.WriteLine(items[i] + " : " + prices[i]);
            }
            List<List<string>> temp = new List<List<string>>();
            temp.Add(items);
            temp.Add(prices);
            temp.Add(imgUrls);
            temp.Add(finalIds);
            return temp;
        }

        public static List<List<string>> ExtractDataV2(string data)
        {

            List<string> items = new List<string>();
            List<string> prices = new List<string>();
            List<string> imgUrls = new List<string>();
            List<string> ids = new List<string>();
            List<string> clubPrices = new List<string>();
            dynamic data2 = JObject.Parse(data);
            dynamic t = data2.search;
            t = t.data;
            t = t.results;
            t = t.productItems;
            foreach (dynamic product in t)
            {
                int index = 0;
                foreach(dynamic part in product)
                {
                    if(index == 0)
                    {
                        dynamic temp = part.First;
                        if(temp.Count != 0) { 

                        temp = temp.First;
                        
                            string clubPrice = temp.offerText;
                            string final = "";
                            bool startPrice = false;
                            string firstThree = "";
                            string firstFive = "";
                            int indexChar = 0;
                            foreach(char c in clubPrice)
                            {
                                if(indexChar < 3)
                                {
                                    firstThree += c.ToString().ToLower();
                                    
                                }
                                if(indexChar < 5)
                                {
                                    firstFive += c.ToString().ToLower();
                                    indexChar++;
                                }
                                if(c.ToString() == "£")
                                {
                                    startPrice = true;
                                    continue;
                                }
                                if (startPrice) {
                                if (c.ToString() == " ")
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        final += c.ToString();
                                    }
                                }
                            }
                            if (firstThree != "any" && firstFive != "tesco")
                            {
                                clubPrices.Add(final);
                            }
                            else
                            {
                                clubPrices.Add("null2");
                            }
                        }
                        else
                        {
                            clubPrices.Add("null");
                        }
                    }
                    if(index == 3)
                    {
                        dynamic temp = part.First;
                        int index2 = 0;
                        foreach(dynamic temp2 in temp)
                        {
                            if(index2 == 2)
                            {
                                ids.Add(temp2.First.ToString());
                            }
                            else if(index2 == 6)
                            {
                                items.Add(temp2.First.ToString());
                            }
                            else if(index2 == 9)
                            {
                                imgUrls.Add(temp2.First.ToString());
                            }
                            else if(index2 == 39)
                            {
                                prices.Add(temp2.First.ToString());
                            }
                            index2 += 1;
                        }
                        
                    }
                    index++;
                }

            }
            List<string> finalIds = new List<string>();
            foreach (string id in ids)
            {
                string finalId = "";
                foreach (char c in id)
                {
                    if (c.ToString() != "\"")
                    {
                        finalId += c;
                    }
                }
                try
                {
                    int test = Convert.ToInt32(finalId);
                }
                catch (Exception)
                {
                    continue;
                }
                finalIds.Add(finalId);
                // Console.WriteLine(items[i] + " : " + prices[i]);
            }
            List<List<string>> temp4 = new List<List<string>>();
            temp4.Add(items);
            temp4.Add(prices);
            temp4.Add(imgUrls);
            temp4.Add(finalIds);
            temp4.Add(clubPrices);
            return temp4;
        }
        public static int SQLCreateAccount(string username,string password)
        {
            try
            {
                Create(username,password);
            }catch(Exception e)
            {
                return -2;
            }
            return 0;
        }
        public static async void Create(string username,string password)
        {
			DocumentReference docRef = db.Collection("users").Document();
			Dictionary<string, object> user = new Dictionary<string, object>
			{
				{ "username", username },
				{ "password", password },
				{ "orderId","null" },
                {"real",true }
			};
			await docRef.SetAsync(user);
		}
        
        public static async Task<bool> SQLCheckAccount(string username)
        {
            bool found = false;
			Query capitalQuery = db.Collection("users").WhereEqualTo("real", true);
			QuerySnapshot capitalQuerySnapshot = await capitalQuery.GetSnapshotAsync();
			foreach (DocumentSnapshot documentSnapshot in capitalQuerySnapshot.Documents)
			{
				Dictionary<string, object> city = documentSnapshot.ToDictionary();
				foreach (KeyValuePair<string, object> pair in city)
				{
					Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
                    if(pair.Key == "username")
                    {
                        if (pair.Value.ToString() == username)
                        {
                            found = true;
                        }
                    }
				}
			}
            return found;
		}
		public static async Task<List<object>> Login(string username,string password)
		{
			bool correct = false;
            string id = null;
			try
            {
                
                Query usersQuery = db.Collection("users").WhereEqualTo("real", true);
                QuerySnapshot usersQuerySnapshot = await usersQuery.GetSnapshotAsync();
                foreach (DocumentSnapshot documentSnapshot in usersQuerySnapshot.Documents)
                {
                    
                    Dictionary<string, object> users = documentSnapshot.ToDictionary();
                    bool user = false;
                    bool pass = false;
                    foreach (KeyValuePair<string, object> pair in users)
                    {
                        Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
                        if (pair.Key == "username")
                        {
                            if (pair.Value.ToString() == username)
                            {
                                user = true;
                            }
                        }
                        if (pair.Key == "password")
                        {
                            if (pair.Value.ToString() == password)
                            {
                                pass = true;
                            }
                        }

                    }
                    if (user && pass)
                    {
                        id = documentSnapshot.Id;
                        correct = true;
                    }
                }
            }
            catch (Exception)
            {
				List<object> temp2 = new List<object>();
				temp2.Add(-2);
				temp2.Add("null");
				return temp2;
			}
			if (!correct)
			{
				List<object> temp3 = new List<object>();
				temp3.Add(-1);
				temp3.Add("null");
				return temp3;
			}
            else
            {
				List<object> temp4 = new List<object>();
				temp4.Add(id);
				temp4.Add(username);
				return temp4;
			}
		}

    
        public static int PutRequest(string productId,string itemName)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://www.tesco.com/groceries/en-GB/trolley/items?_method=PUT");
            httpWebRequest.Method = "PUT";
            httpWebRequest.Headers["Cookie"] = "atrc=7018fe14-25aa-4b69-afb1-deec4dad0e2a; h-e=ec5a7f09a46b825f424c83ecad5ee4ff4ef3f09b188a7cb7f6719ff50f07a0b2; trkid=ed990236-5fb4-4166-ae7e-9ead9a6801b8; _abck=C7A1A48549CEF2D36520306165E15951~-1~YAAQVkwQAmH5KH6EAQAArc5g9glr4FzOimnlKytj4puOlEkxFvt3wjxgPiT3AbOH9pCVM8v/zZPMKY0riaJce0mYUDde5nJyjw3xnJQTIR/H1dR1amWTcyvKk5ktibQcyXDlY7W6YPHJYum2PH4unSgl66C9PLzM2ttBpUg2WiHA4yttA9oYHQmx9aefpmbMH9XXAVj2qg1gv8si8ksy4JMfH2Fw6716706qlynZGLs7bXHANJdvv1X0akApyk/Dzlx3wNjA1d47KRsecO7mHmrTNYOh9U5zWcO7ZyaVLZgk39I/6QsR2cSOznEMadl82r4Zz4gH3UaW1n4wyDO867MSbfyxnXYIi3q7YeP2OxMNft7kRsOs/gv14wBtDsEx9d86sTfvBDXyMqCkqgT+UsOF41DwGQw=~0~-1~1670584282; atrc=9b9c5a37-4cfc-43c8-8841-12ecb3bd912b; _csrf=Ag15oY6ER-yN2zw-Vpz4xdzMnUSnS3Y9QQzjw";

			httpWebRequest.Headers["authority"] = "www.tesco.com";
            httpWebRequest.Accept = "application/json";
            httpWebRequest.Headers["accept-language"] = "en-GB,en-US;q=0.9,en;q=0.8";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Headers["cookie"] = "null; null; null; consumer=default; trkid=ed990236-5fb4-4166-ae7e-9ead9a6801b8; atrc=7018fe14-25aa-4b69-afb1-deec4dad0e2a; DCO=sdc; h-e=ec5a7f09a46b825f424c83ecad5ee4ff4ef3f09b188a7cb7f6719ff50f07a0b2; deliveryPreferences=j%3A%7B%22addressId%22%3A%22104610347%22%7D; ighs-sess=eyJwYXNzcG9ydCI6eyJ1c2VyIjp7ImlkIjoiQ2FsbHVtMTIzNDUuY29tQGdtYWlsLmNvbSIsImVtYWlsIjoiSzdHcXBpMWlBblpSd2VpVk5BUkdYNDdzbW4rYlh3LzV0RlhHb1pwZ1dBQT0ifX0sImFuYWx5dGljc1Nlc3Npb25JZCI6IjUyZjg1NmYwNDMwN2JhMDBhNWM2ODljNGZkN2JkN2YwIiwic3RvcmVJZCI6IjU5MzAiLCJ1dWlkIjoiNTJmMmU2YjQtMzkwNy00NmIyLWE2N2QtYTMwMjMzMzc4NGZlIn0=; ighs-sess.sig=519mHfZjte6aQFsnA5uuZszAWLc; null; itemsPerPage=48; atrc=df9f9034-47d9-4ffa-b50b-52e707290edb; _csrf=rvuvUUpGwD_0zLd27YIu1QfA; cookiePreferences=%7B%22experience%22%3Atrue%2C%22advertising%22%3Atrue%7D; _gcl_au=1.1.175648936.1670716074; AMCVS_E4860C0F53CE56C40A490D45%40AdobeOrg=1; s_cc=true; _mibhv=anon-1670580767025-9014973469_4481; _4c_mc_=bb336543-3d61-4324-93b6-d9db09c3b08b; optimizelyEndUserId=oeu1670946505908r0.7781257664873842; HYF=1; UUID=52f2e6b4-3907-46b2-a67d-a302333784fe; bm_sz=745B6BDE26B578B096D8D782797B7BF0~YAAQCTZ6XOJIHkCGAQAA+iTZQRIcV4YVX8kMp7rAbxGWI62853AtPU4QMUgyq6ZvExrrpaQ4XQnay9viT54kC4qeXz/C+2vinmPuQt5OmKYiJuOXl0a1LS4sZm3hCsvGNC14zOyfW+HLSr4QLt40UAoz6TvHOLRqSaY/FPZaDf2OgTQb6XURVPt03xW8dbygrGTB4MUmROnrVo++HdpZvRi2nDUkEU0M5sa2EgP3GvluXyVw1cqc8/moCkLbv4UQUSrgTn4ufSyBlIyfTV8B/RRjew1IPvdIzhM26Q+G23mg1Q==~4601142~3289668; ak_bmsc=6D7CFC03D8B79A229A475731AFDE838B~000000000000000000000000000000~YAAQCTZ6XMtJHkCGAQAAXCzZQRJK5iXdose60tWVnQ7Ai0RYQ5y26BdDtCfvzQ64lMjvN97ejL5THIPADhZJHZnvehxdFNkJIt6NKutj8y9nBFD2J4Ux4yOxts7adzfAP3D6ehkOMZWVT01qXyQTIWgyPHwt71o/yagXxtp2Il3W4DlB0FKfewhUXpDmwhF98roFa7lUC3gtkJcKCvv2pUUHKM1/HtNmelNAx2O5Cx179BhWYYiwC74jWaEzpHJIv6tgKUv9o6uB6fvs7gAy7oHTPRd+twJl34ncgy/KFxXZmSV7OgsY8dXJYSeInagSmoIA5cppYigB/cW2FnP1i3Q3GaM19VnJ0XrFppBYJVrv0RlEp3ROr8OQSLwFg2cjgObyeBXC/OnEFtfummIO9gSs4ZLbwkQuE2h30SSTDXSbWgbtZoaBSJFSPynv9clYrbWPmevZCgNf30qdyoUa2FNL4iCxuvcIR4snTvIQO+pSf+jrE2zjl62n; AMCV_E4860C0F53CE56C40A490D45%40AdobeOrg=1585540135%7CMCIDTS%7C19400%7CMCMID%7C89858544417324752138609249372332897292%7CMCAID%7CNONE%7CMCOPTOUT-1676149198s%7CNONE%7CvVersion%7C4.4.0%7CMCAAMLH-1676746797%7C6%7CMCAAMB-1676746797%7Cj8Odv6LonN4r3an7LhD3WZrU1bUpAkFkkiY1ncBR96t2PTI; _gid=GA1.2.439564286.1676141998; CID=109635388; s_nr=1676142011375-Repeat; AKA_A2=A; OAuth.AccessToken=eyJraWQiOiIyMTM1MGE2NC02YTYxLTRiNmYtYjIyZS1lYTNkOTNiY2YyMTQiLCJhbGciOiJSUzI1NiJ9.eyJqdGkiOiI3N2I3NzdkZS1kNjk3LTQ2ZGUtOTNmZC04NjQ1NTE3ODVhNDciLCJpc3MiOiJodHRwczovL2FwaS50ZXNjby5jb20vaWRlbnRpdHkvdjQvaXNzdWUtdG9rZW4iLCJzdWIiOiI1MmYyZTZiNC0zOTA3LTQ2YjItYTY3ZC1hMzAyMzMzNzg0ZmUiLCJpYXQiOjE2NzYxNDcyNjcsIm5iZiI6MTY3NjE0NzI2NywiZXhwIjoxNjc2MTUwODY3LCJzY29wZSI6ImludGVybmFsIiwiY29uZmlkZW5jZV9sZXZlbCI6MTIsImNsaWVudF9pZCI6ImExNzE0NzBlLWIwZTMtNDZkMS1iZjEyLWMyMGI0MTI4ZjRiNiIsInRva2VuX3R5cGUiOiJiZWFyZXIiLCJzaWQiOiIwMUdTMFhKRzFWV0hWWkJDV1I3Q0FZOFlKUi0xMDA0ODFlOC1kNWU2LTQ1YmUtYTQ5Ny02YzY4YTQ1NzNhNTMta0tsdGVaOHBXeF9SUGJUR0xxUkdLOHU3anlFTE9OTkUyenpGIn0.eS2SY1x4P6rVoi8uH1vkFQXnxLf2Q4qXQ-8G71fAkR4ajIX35P17dGcFvOmWMB2XUa268EUesBb7cbGp3A6zvL2V9_Zbw2uUiMuFkPYoKoaEutz9OqZlhg3AnrcBn8fmoLwveRdkA0f0qHLy4kO8Uc6NBMvMgy5v9yeqWw8L-QtrlPsQklU3xJPO_Fu9YVNc7PQsgArizOtzcrJ85NLigWFpuNiHeihchVgGpSX0UD15B30GY10hqYl0FNx21EySRyStu8PTnHsa4bQBLu7MyEeckzDYVBrc1iUgMwWJOKX7XAd7WpKdcbJKgpCkooEuDVRouCZJTLxBh2KeU7ZrZQ; OAuth.RefreshToken=e936af99-16df-4f78-8503-dacd702f09c6; OAuth.TokensExpiryTime=%7B%22AccessToken%22%3A1676150866886%2C%22RefreshToken%22%3A1676150866886%7D; akavpau_OneAccount=1676147568~id=512cb00ef3867e1de858b1a02d76b49c; da_sid=9C0CA2EB8E38AE8D2686AA13B24B91F1F3|4|0|4; da_lid=90FC84D19A73EA10B0A5BB99F5B6BD09F6|0|0|0; da_intState=; _ga=GA1.2.725422933.1670716073; akavpau_tesco_groceries=1676147937~id=d0eec948b63b8fb89bc0b227d01d5cd6; _abck=DB3EA9072434AD7B6985E6052305F9F6~0~YAAQCTZ6XHwmJ0CGAQAAIT0vQglPX/MnFwje+LgoBX2STlTlGyY5LxVfs94wXw7R+dmSLOFCJgyxFaY1OdLw2gfWV6+CEIpUowCxT0Dtiv4Y7gbHaqhgexzUpESddG2x7sxruJhJBjXjfZYVT1lv0De6SL+IoKSJpjyNKzkMqEiRz9RFQDSoKCsYr7OF29c1QdA118ZAT1FRxqMctEcAdJ0NUv09pVE68PCHuZJvwqgGtdqouHybFpsKovdc6QegoKt10vk46S/OzlfGaZXqk+UnvC9r0yzHKev2hZaJr4KYwW27JRUWo0ffFRiFIlK1ZK3NF2odf+CyAocdbJNy/XOhovPZ0Wm/EJkNFZoQ63C8UcRR4dU4kz5Dd249FflQjI84fRU91qcBYCD54/NLGl5Pl4x8Oe4=~-1~-1~1676151229; s_gpv_pn=search%20results; bm_sv=45498FC74E0902F10B3F05EB43E6634B~YAAQCTZ6XJ8oJ0CGAQAA204vQhKr9yuQ7whLsriYNgs7hpGWljLXz1+nFvuwTc4Fg00LVh/RreXNC89uwdzoG649eMbLhwKHYOv5nCz1hPAbjUaLpnSMCzty8gFDvVDKrYlkRSBXZzoB+KT/JGxdkMJ620g+aOZxPB2utKipv0gHmHhvtrcuGczQRBz/s25rxu6bDcsTneDaemjew8VmFYCzhoyHBo/yt70y/BTVrm2GJvS/Tl3J+2pBtE/PQuM7~1; _uetsid=52b5ed00aa3e11edba034d37b931058d; _uetvid=0724a1b077aa11ed9f1a3321eb13f478; _gat_ukLego=1; _ga_33B19D36CY=GS1.1.1676147263.17.1.1676147643.60.0.0";
            httpWebRequest.Headers["origin"] = "https://www.tesco.com";
            httpWebRequest.Referer = "https://www.tesco.com/groceries/en-GB/search?query=" + itemName;
            httpWebRequest.Headers["sec-ch-ua"] = "\"Not_A Brand\";v=\"99\", \"Google Chrome\";v=\"109\", \"Chromium\";v=\"109\"";
            httpWebRequest.Headers["sec-ch-ua-mobile"] = "?0";
            httpWebRequest.Headers["sec-ch-ua-platform"] = "\"Windows\"";
            httpWebRequest.Headers["sec-fetch-dest"] = "empty";
            httpWebRequest.Headers["sec-fetch-mode"] = "cors";
            httpWebRequest.Headers["sec-fetch-site"] = "same-origin";
            httpWebRequest.Headers["traceparent"] = "00-c34649dd6e71ba0b3070a813aed042cb-e6d1996f5f3edccd-01";
            httpWebRequest.Headers["tracestate"] = "3296235@nr=0-1-3512954-1134246125-e6d1996f5f3edccd----1676147675438";
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";
            httpWebRequest.Headers["x-csrf-token"] = "AyjWVz74-uh4VcxFqa0T_qaeAQmw6haLkfsk";
            httpWebRequest.Headers["x-requested-with"] = "XMLHttpRequest";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.Headers["Accept-Encoding"] = "gzip, deflate, br";
            httpWebRequest.Headers["x-requested-with"] = "XMLHttpRequest";
            httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"items\":[{\"id\":\""+productId+"\",\"newValue\":1,\"oldValue\":0,\"newUnitChoice\":\"pcs\",\"oldUnitChoice\":\"pcs\",\"substitutionOption\":\"FindSuitableAlternative\"}],\"returnUrl\":\"/groceries/en-GB/search?query=" + itemName + "\"}";
                streamWriter.Write(json);
            }
            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    return 0;
                }
            }
            catch (Exception e)
            {
                return -1;
            }
            return -1;
        }

        public static async Task<string> GetPrice(string orderId)
        {
            string total = "";
			Query ordersQuery = db.Collection("orders").WhereEqualTo("live", true);
			QuerySnapshot ordersQuerySnapshot = await ordersQuery.GetSnapshotAsync();
			foreach (DocumentSnapshot documentSnapshot in ordersQuerySnapshot.Documents)
			{
                if (documentSnapshot.Id == orderId)
                {
                    Dictionary<string, object> users = documentSnapshot.ToDictionary();
                    bool user = false;
                    bool pass = false;
                    foreach (KeyValuePair<string, object> pair in users)
                    {
                        Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
                        if (pair.Key == "total")
                        {
                            total = pair.Value.ToString();
                            
                        }
                        

                    }
                }
			}
            return total;

		}
       
        public static async Task<string> GetDate(string orderId)
        {
			string total = "";
			Query ordersQuery = db.Collection("orders").WhereEqualTo("live", true);
			QuerySnapshot ordersQuerySnapshot = await ordersQuery.GetSnapshotAsync();
			foreach (DocumentSnapshot documentSnapshot in ordersQuerySnapshot.Documents)
			{
				if (documentSnapshot.Id == orderId)
				{
					Dictionary<string, object> users = documentSnapshot.ToDictionary();
					bool user = false;
					bool pass = false;
					foreach (KeyValuePair<string, object> pair in users)
					{
						Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
						if (pair.Key == "date")
						{
							total = pair.Value.ToString();

						}


					}
				}
			}
			return total;
		}
        public static async Task<int> AddToOrderV2(int productId,float cost, string orderId, string userId, string query,string name,string img)
        {
            try
            {
                double price = double.Parse((await GetPrice(orderId)));
                price += cost;
                price = Math.Round(price, 2);
                double finalCost = Math.Round(cost, 2);
                Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"added",false },
                {"query",query },
                {"owner",userId },
                {"productId",productId },
                {"price",finalCost },
                {"name",name},
                {"image",img }
            };
                Dictionary<string, object> dataToDelete = new Dictionary<string, object>
                {
                    {"products",FieldValue.Delete }
                };

				List<object> theProducts = await GetProducts(orderId);
				theProducts.Add(data);
				DocumentReference orderRef = db.Collection("orders").Document(orderId);
                await orderRef.UpdateAsync(dataToDelete);
                await orderRef.UpdateAsync("products", theProducts);
                await orderRef.UpdateAsync("total", price.ToString());
            }
            catch(Exception)
            {
                return -2;
            }
            return 0;







		}
        /*public static int AddToOrder(int productId,float cost, int orderId,string userId,string query)
        {
            string currentProduct = GetProducts(orderId);
            double price = (double)(GetPrice(orderId));
            string priceNormalised = "";
            foreach(char c in cost.ToString())
            {
                if(c.ToString() == ".")
                {
                    priceNormalised +="!";
                }
                else
                {
                    priceNormalised += c.ToString();
                }
            }
            if(currentProduct == "null" || price == -1)
            {
                return -1;
            }
            if (currentProduct == "")
            {
                char[] productFinal = new char[currentProduct.Length + productId.ToString().Length + userId.Length + query.Length + cost.ToString().Length + 4];
                string value = productId.ToString() + "N." + userId.ToString() + "." + priceNormalised + "." + query;
                for (int i =0; i<value.Length; i++)
                {
                    productFinal[i] = value[i];
                }
                currentProduct = string.Concat(productFinal);
            }
            else
            {
                string value = productId.ToString() + "N." + userId.ToString() + "." + priceNormalised + "." + query+",";
                char[] productFinal = new char[currentProduct.Length + productId.ToString().Length + userId.Length + query.Length + cost.ToString().Length + 5];
                int i = 0;
                for (i = 0; i < value.Length; i++)
                {
                    productFinal[i] = value[i];
                }
                for(int j = 0; j < currentProduct.Length; j++)
                {
                    productFinal[i] = currentProduct[j];
                    i++;
                }
                currentProduct = string.Concat(productFinal);
            }
            price += cost;
            price = Math.Round(price, 2);
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "cufesqlserver.database.windows.net";
                builder.UserID = "azureuser";
                builder.Password = "Cufe582458!";
                builder.InitialCatalog = "TescoDatabase";
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    string sql = "update orders set productId =\'"+currentProduct+"\',price = "+price+" where orderId = "+orderId;
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {

                        }
                    }
                }
            }
            catch (SqlException e)
            {
                
                return -2;
            }
            return 0;
        }

        */
       /* public static float GetPrice(int orderId)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "cufesqlserver.database.windows.net";
                builder.UserID = "azureuser";
                builder.Password = "Cufe582458!";
                builder.InitialCatalog = "TescoDatabase";
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    string sql = "SELECT price from orders where orderId =" + orderId;
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            int account = -1;
                            while (reader.Read())
                            {
                                if (reader.HasRows)
                                {
                                    var temp = reader.GetValue(0);
                                    return float.Parse(reader.GetValue(0).ToString());
                                }


                            }

                            return -1;


                        }
                    }
                }
            }
            catch (SqlException e)
            {
                return -1;
            }
        }
        
        */
        public async static Task<List<object>> GetProducts(string orderId)
        {
            List<object> products = new List<object>();
            try
            {
                Query ordersQuery = db.Collection("orders").WhereEqualTo("live", true);
                QuerySnapshot ordersQuerySnapshot = await ordersQuery.GetSnapshotAsync();
                foreach (DocumentSnapshot documentSnapshot in ordersQuerySnapshot.Documents)
                {
                    if (documentSnapshot.Id == orderId)
                    {
                        Dictionary<string, object> users = documentSnapshot.ToDictionary();
                        foreach (KeyValuePair<string, object> pair in users)
                        {
                            if (pair.Key == "products")
                            {
                                products = (List<object>)pair.Value;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                products = new List<object>();
                products.Add("");
                return products;
            }
            if(products == null || products.Count == 0)
            {
				products = new List<object>();
				products.Add("null");
				return products;
            }
            return products;
            
        }
        
       public async static Task<int> UpdateProducts(List<object> products,string orderId)
        {
			List<object> allProducts = await GetProducts(orderId);
            if(allProducts.Count == 0 || allProducts == null) {
                return -1;
            }
			try
            {
                DocumentReference ordersRef = db.Collection("orders").Document(orderId);
                for (int i = 0; i < allProducts.Count; i++)
                {
                    object product = (object)allProducts[i];
                    string id = "";
                    string owner = "";
                    foreach (KeyValuePair<string, object> fields in (Dictionary<string, object>)product)
                    {
                        if (fields.Key == "productId")
                        {
                            id = fields.Value.ToString();
                        }
                        if (fields.Key == "owner")
                        {
                            owner = fields.Value.ToString();
                        }
                    }

                    for (int j = 0; j < products.Count; j++)
                    {
                        object product2 = (object)products[j];
                        bool idV = false;
                        bool nameV = false;
                        foreach (KeyValuePair<string, object> fields2 in (Dictionary<string, object>)product)
                        {
                            if (fields2.Key == "productId")
                            {
                                if (fields2.Value.ToString() == id)
                                {
                                    idV = true;
                                }
                            }
                            if (fields2.Key == "owner")
                            {
                                if (fields2.Value.ToString() == owner)
                                {
                                    nameV = true;
                                }
                            }
                        }
                        if (idV && nameV)
                        {
                            Dictionary<string, object> temp = (Dictionary<string, object>)allProducts[i];

                            temp["added"] = (object)true;
                            allProducts[i] = temp;
                            products.RemoveAt(j);
                            break;
                        }
                    }

                }
				DocumentReference orderRef = db.Collection("orders").Document(orderId);
				Dictionary<string, object> fieldDelete = new Dictionary<string, object>
				{
					{"products",FieldValue.Delete }
				};
				await orderRef.UpdateAsync(fieldDelete);
				await orderRef.UpdateAsync("products", allProducts);






			}
            catch (Exception)
            {
                return -2;
            }
            return 0;
        }
      
        /*  public static int UpdatePrice(float price, int orderId)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = "cufesqlserver.database.windows.net";
                builder.UserID = "azureuser";
                builder.Password = "Cufe582458!";
                builder.InitialCatalog = "TescoDatabase";
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    string sql = "update orders set price =\'" + Math.Round(price, 2) + "\' where orderId = " + orderId;
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {

                        }
                    }
                }
            }
            catch (SqlException e)
            {

                return -2;
            }
            return 0;
        }
      */
        public async static Task<int> AddToCart(string orderId)
        {
            List<object> products = await GetProducts(orderId);
            if (products[0] == "null")
            {
                return -1;
            }
            List<object> addedProducts = new List<object>();
			for (int i = 0; i < products.Count; i++)
			{
				object product = (object)products[i];
				string id = "";
				string query = "";
				bool add = false;
				foreach (KeyValuePair<string, object> fields in (Dictionary<string, object>)product)
				{
                    if(fields.Key.ToString() == "added")
                    {
                        
                        if(fields.Value.ToString().ToLower() == "false")
                        {
                            add = true;
                        }
                        
                    }
                    if(fields.Key == "query")
                    {
                        query = fields.Value.ToString();
                    }
                    if(fields.Key == "productId")
                    {
                        id = fields.Value.ToString();
                    }
                }
                if (add)
                {
                    addedProducts.Add(product);
                    int result = PutRequest(id, query);
                    if (result == -1)
                    {
                        return -3;
                    }
                }
            }
            return await UpdateProducts(addedProducts, orderId);
        }
        
        public async static Task<int> AddOrder(string date)
        {
            try
            {
                DocumentReference orderRef = db.Collection("orders").Document();
                Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"date",date },
                {"live",true},
                {"total","0.00"}
                    //might need to add  products check!!!
            };

                await orderRef.SetAsync(data);

            }
            catch (Exception)
            {
                return -1;
            }
            return 0;
        }
        
        public async static Task<List<List<string>>> FetchOrders()
        {
			List<List<string>> orders = new List<List<string>>();
			try
            {
                
                Query ordersQuery = db.Collection("orders").WhereEqualTo("live", true);
                QuerySnapshot ordersQuerySnapshot = await ordersQuery.GetSnapshotAsync();
                foreach (DocumentSnapshot documentSnapshot in ordersQuerySnapshot.Documents)
                {
                    string[] order = new string[3];
                    order[0] = (documentSnapshot.Id);
                    Dictionary<string, object> users = documentSnapshot.ToDictionary();
                    foreach (KeyValuePair<string, object> pair in users)
                    {
                        if (pair.Key == "date")
                        {
                            order[1] = pair.Value.ToString();

                        }
                        if (pair.Key == "total")
                        {
                            order[2] = pair.Value.ToString();
                        }


                    }

                    List<string> temp = new List<string>();
                    foreach (string a in order)
                    {
                        temp.Add(a);
                    }
                    orders.Add(temp);
                }
                return orders;
            }
            catch (Exception)
            {

            }
            List<string> temp2 = new List<string>();
            temp2.Add("ERROR");
            List<List<string>> temp3 = new List<List<string>>();
            temp3.Add(temp2);
            return temp3;
        }
    
        public async static Task<List<List<string>>> FetchProducts(string orderId)
        {
            List<object> products = await GetProducts(orderId);
            List<string> productsList = new List<string>();
            List<string> queryList = new List<string>();
            List<string> userList = new List<string>();
            List<string> priceList= new List<string>();
            List<string> nameList = new List<string>();
            List<string> imageList = new List<string>();
            List<List<string>> finalProductsList = new List<List<string>>();
            if (products[0] == "null")
            {
                List<List<string>> error = new List<List<string>>();
                List<string> error1 = new List<string>();
                error1.Add("-1");
                error.Add(error1);
                return error;
            }
            foreach(object product in products)
            {
                int gfjh = 0;
                foreach (KeyValuePair<string, object> fields in (Dictionary<string, object>)product)
                {
                    if (fields.Key == "productId")
                    {
                        productsList.Add(fields.Value.ToString());
                    }
                    if (fields.Key == "query")
                    {
                        queryList.Add(fields.Value.ToString());
                    }
                    if (fields.Key == "owner")
                    {
                        userList.Add(fields.Value.ToString());
                    }
                    if (fields.Key == "image")
                    {
                        imageList.Add(fields.Value.ToString());
                    }
                    if (fields.Key == "name")
                    {
                        nameList.Add(fields.Value.ToString());
                    }
                    if(fields.Key == "price")
                    {
                        priceList.Add(fields.Value.ToString());
                    }
                }
            }
			finalProductsList.Add(userList);
			finalProductsList.Add(priceList);
			finalProductsList.Add(productsList);
            finalProductsList.Add(nameList);
            finalProductsList.Add(imageList); 
            
            
            
            return finalProductsList;
            
        }

        //Extracts the products and queries into two lists from the long string stored in database
        /*  public static List<List<string>> ExtractProducts(string products,bool wantName,string name)
          {
              List<string> productsList = new List<string>();
              List<string> queryList = new List<string>();
              List<string> priceList = new List<string>();
              List<string> userList = new List<string>();
              bool canAdd = true;
              string temp = "";
              bool idFound = false;
              bool first = false;
              bool second = false;
              bool third = false;
              bool toRemove = false;
              foreach (char c in products)
              {
                  if (c.ToString() == ",")
                  {
                      queryList.Add(temp);
                      temp = "";
                      canAdd = true;
                      temp = "";
                      idFound = false;
                      first = false;
                      second = false;
                      third = false;
                      toRemove = false;
                  }
                  else if (canAdd)
                  {
                      if (!idFound)
                      {
                          try
                          {
                              int test = Convert.ToInt32(c.ToString());
                              temp += c.ToString();


                          }
                          catch (Exception)
                          {
                              idFound = true;
                              if (c.ToString() == "N" || c.ToString() == "Y")
                              {
                                  productsList.Add(temp);
                              }
                              temp = "";
                          }
                      }
                      else
                      {
                          if (!first)
                          {
                              if (c.ToString() == ".")
                              {
                                  first = true;
                              }
                          }
                          else
                          {
                              if (!second)
                              {


                                  if (c.ToString() == ".")
                                  {
                                      second = true;
                                      if (wantName)
                                      {
                                          userList.Add(temp);
                                          if (temp != name && name !="n/a")
                                          {
                                              productsList.RemoveAt(productsList.Count - 1);
                                              toRemove = true;
                                          }
                                          temp = "";
                                      }
                                  }
                                  else
                                  {
                                      if (wantName)
                                      {
                                          temp += c.ToString();

                                      }
                                  }
                              }
                              else
                              {
                                  if (!third)
                                  {
                                      if(c.ToString() == ".")
                                      {
                                          if (!toRemove)
                                          {
                                              priceList.Add(temp);
                                          }
                                          temp = "";
                                          third = true;
                                      }
                                      else
                                      {
                                          temp += c.ToString();
                                      }
                                  }
                                  else
                                  {
                                      if (c.ToString() == ".")
                                      {
                                          queryList.Add(temp);
                                          temp = "";
                                      }
                                      else
                                      {
                                          temp += c.ToString();
                                      }


                                  }
                              }

                          }
                      }

                  }
              }
              queryList.Add(temp);
              List<List<string>> result = new List<List<string>>();
              result.Add(productsList);
              result.Add(queryList);
              result.Add(priceList);
              result.Add(userList);
              return result;
          }

          */
        public async static Task<int> RemoveProduct(float price, int productId, string orderId, string userId)
        {
            List<object> products = await GetProducts(orderId);
            double totalPrice = double.Parse(await GetPrice(orderId));
            totalPrice -= price;
            totalPrice = Math.Round(totalPrice, 2);
            int originalLength = products.Count;
            try
            {

                int uhdfjdf = 0;
                for(int i = 0; i<products.Count; i++)
                {
                    object product = (object)products[i];

					if (originalLength != products.Count)
                    {
                        break;
                    }
                    string owner = "";
                    object tempProduct = null;
                    foreach (KeyValuePair<string, object> fields in (Dictionary<string,object>)product)
                    {
                        if (fields.Key == "productId")
                        {
                            if (int.Parse(fields.Value.ToString()) == productId)
                            {
                                tempProduct = product;
                            }
                        }
                        if (fields.Key == "owner")
                        {
                            owner = fields.Value.ToString();
                        }
                    }
                    if (owner == userId && tempProduct != null)
                    {
                         products.RemoveAt(i);
                        break;
                    }
                }



            
                if (originalLength != products.Count)
                {
                    DocumentReference orderRef = db.Collection("orders").Document(orderId);
					Dictionary<string, object> fieldDelete = new Dictionary<string, object>
				{
					{"products",FieldValue.Delete }
				};
                    await orderRef.UpdateAsync(fieldDelete);
					await orderRef.UpdateAsync("products", products);
                    await orderRef.UpdateAsync("total", totalPrice.ToString());
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception)
            {
                return -1;
            }
            return 0;

        }
    
        public async static Task<double> GetUserPrice(string userId,string orderId)
        {
            List<object> products = await GetProducts(orderId);
            double price = 0.00;
            if (products[0] == "null")
            {
                return 0.00;
            }
            foreach (object product in products)
            {
                double cost = 0.00;
                bool add = false;
                foreach (KeyValuePair<string, object> fields in (Dictionary<string, object>)product)
                {
                    if(fields.Key == "owner")
                    {
                        if(fields.Value.ToString() == userId)
                        {
                            add = true;
                        }
                    }
                    if(fields.Key == "price")
                    {
                        cost = double.Parse(fields.Value.ToString());
                    }
                }
                if (add)
                {
                    price += cost;
                }
            }
            return price;
            

        }
    }
}
