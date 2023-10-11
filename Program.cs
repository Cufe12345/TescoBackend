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

namespace TescoTest 
{
	internal class Program
    {
        //Firebase database credentials
        static string path = AppDomain.CurrentDomain.BaseDirectory + @"YOURCREDENTIALS OF YOUR FIREBASE.json";
        static FirestoreDb db;

        public static HttpClient client;
        static bool runServer = true;
        public static HttpListener listener;

        //Setting the port/s to connect to the backend server on
        public static string[] url = {"http://*:8080/","https://*:8443/"};
        public static bool complete = false;

        
        static void Main(string[] args)
        {
            //Connect to the database
			Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
			 db = FirestoreDb.Create("cufe12345-tescowebsite");
            //Testing();
            if (args.Length != 0)
            {
                if (args[0].ToUpper() == "-O" || args[0].ToUpper() == "-A" || args[0].ToUpper() == "-T" || args[0].ToUpper() == "-H")
                {
                    Menu(args);
                }
            }
            else
            {
                StartServer();
            }
                while (!complete)
                {
                    int a = 0;
                }
            
        }

		/// <summary>
		/// Method <c>Menu</c> Uses the arguments provided to do specific actions chosen
		/// </summary>
		async public static void Menu(string[] args)
        {
            //Adds order to database with the name in args[1] eg if you want the order "First Order" added to front end website
			if (args[0].ToUpper() == "-O")
			{
				Console.WriteLine(await AddOrder(args[1]));
			}
            //Adds the order provided's content to the tesco websites basket 
			else if (args[0].ToUpper() == "-A")
			{
				
                Console.WriteLine("Enter Cookie");
                string Cookie = Console.ReadLine();
				Console.WriteLine("Enter cookie");
				string cookie = Console.ReadLine();
				Console.WriteLine("Enter x-csrf-token");
				string x_csrf_token = Console.ReadLine();
                List<string> parameters = new List<string>
                {
                    Cookie,cookie,x_csrf_token
                };
                Console.WriteLine(parameters[0]);
				Console.WriteLine(await AddToCart(args[1],parameters));

			}
            //Displays the total of the order provide ie args[1]
            else if (args[0].ToUpper() == "-T")
            {
                Console.WriteLine(await Total(args[1]));
            }
            //Tells the user what each argument is and does
            else if (args[0].ToUpper() == "-H")
            {
                Console.WriteLine("-T Total, -A add to cart, -O add order");
            }
            complete = true;
		}
		/// <summary>
		/// Method <c>Total</c> Uses the <paramref name="orderId"/> to fetch all the products and gives a total for every user who has items on the order and overall total
		/// </summary>
		async public static Task<int> Total(string orderId)
        {
            //Fetches all products on order
            List<object> products = await GetProducts(orderId);
            double total = 0;
            Dictionary<string, double> people = new Dictionary<string, double>();
            if (products[0] == "null")
            {
                return -1;
            }
            try
            {
                //Iterates over all products
                List<object> addedProducts = new List<object>();
                for (int i = 0; i < products.Count; i++)
                {
                    object product = (object)products[i];
                    double price = 0;
                    string owner = "";

                    //Goes through the fields on this product and extracts the owner and price
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
                        //if the person exists in the dict just add to total otherwise add the person to the dictionary with the products price
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
		/// <summary>
		/// Method <c>Testing</c> Test if connected to the database correctly
		/// </summary>
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

		/// <summary>
		/// Method <c>TestServer</c> Test if server was started properly by sending POST request to it
		/// </summary>
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

		/// <summary>
		/// Method <c>StartServer</c> Starts the server and listens on ports provided
		/// </summary>
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
		/// <summary>
		/// Task <c>HandleIncomingConnections</c> When a POST request is recieved depending on what data was sent will do the associated actions and return the result back to the frontend
		/// </summary>
		public static async Task HandleIncomingConnections()
        {
            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse response = ctx.Response;
                
                Console.WriteLine("RECIEVED");
                Console.WriteLine(req.HttpMethod);
                //Options the request needs to follow
                if (req.HttpMethod == "OPTIONS")
                {
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                    response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                    response.AddHeader("Access-Control-Max-Age", "1728000");
                    response.Close();
                }
                else if(req.HttpMethod == "POST")
                {
                    //Needed for CORS reasons otherwise frontend not happy
                    response.AppendHeader("Access-Control-Allow-Origin", "*");
                    Console.WriteLine("{\"Data\": \"WORKED\"}");
                    
                    byte[] data = null;
                    using (System.IO.Stream body = req.InputStream) // here we have data
                    {
                        using (var reader = new System.IO.StreamReader(body, req.ContentEncoding))
                        {
                            //Extracts the data in request into values ie values[0] is what you want to do ie SEARCH , CREATE then values[1] is the data associated with the action eg SEARCH Pizza values[1] = Pizza
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
                                    //Fetches all products returned from the search of values[1] on tescos website
                                    //Returns it as JSON objects back to the front end
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
                                                generatedResponse += "{\"price\" : \"" + finalPrice + " / " + postResults[4][i] + " ClubCard\",";

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
                            //Used to create accounts on the database
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
                                //returns if successful or not and if not what was wrong ie not unique username etc
                                int value = await CreateAccount(username, password);
                                data = Encoding.UTF8.GetBytes("{ \"Result\": \"" + value.ToString() + "\"}");
                            }
                            //Logs the user in or returns error value if invalid credentials provided
                            else if (values[0] == "LOGIN")
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
                                List<object> value = await Login(username, password);
                                data = Encoding.UTF8.GetBytes("[{ \"Result\": \"" + value[0].ToString() + "\"}, {\"Name\": \"" + value[1].ToString() + "\"}, {\"admin\":\"" + value[2].ToString() + "\"}]");
                            }
                            //Fetches all orders in database and returns as JSON object to front end
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
                            //adds product to order in database
                            else if (values[0] == "ADD")
                            {

                                //handles the extraction of all data into individual variables ie orderId userId etc
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
                                                foreach (char ch in word)
                                                {
                                                    if (indexPrice < 4)
                                                    {
                                                        initialPrice += ch;
                                                        indexPrice++;
                                                    }
                                                    if (ch.ToString() == "/")
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
                                                            if (ch.ToString() == " ")
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
                                        if (c.ToString() == ",")
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
                                        if (c.ToString() == ",")
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
                                    //Attempts to add product to order in database
                                    //Returns result to frontend
                                    int result = await AddToOrderV2(id, cost, orderId, userId, query, name, image);

                                    string generatedResponse = "";
                                    generatedResponse += "{\"result\" : " + result + "}";
                                    data = Encoding.UTF8.GetBytes(generatedResponse);


                                }
                            }
                            //Fetches the products for an order ie the basket
                            else if (values[0] == "BASKET")
                            {
                                string orderId = "";
                                string name = "";
                                string current = "";
                                for(int j = 0; j < values[1].Length; j++)
                                {
                                    if (values[1][j].ToString() == ",")
                                    {
                                        orderId = current;
                                        current = "";

                                    }
                                    else
                                    {
                                        current += values[1][j].ToString();
                                    }
                                }
                                name = current;
                                if (!(name == "" || orderId == ""))
                                {



                                    if (orderId != null && orderId != "No order")
                                    {
                                        List<List<string>> postResults = await FetchProducts(orderId,name);
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
                                                generatedResponse += " \"title\" : \"" + postResults[0][i] + "\'s " + postResults[3][i] + "\",";
                                                generatedResponse += " \"id\" : \"" + postResults[2][i] + "\",";
                                                generatedResponse += " \"image\" : \"" + postResults[4][i] + "\"}";

                                            }
                                            generatedResponse += "]";
                                            data = Encoding.UTF8.GetBytes(generatedResponse);
                                        }
                                    }
                                }
                            }
                            //Removes a product from an order
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
                                foreach (char c in requestV)
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
                                userId = (word);
                                if (price != -1 && id != -1 && orderId != "-1" && userId != "-1")
                                {
                                    int result = await RemoveProduct(price, id, orderId, userId);
                                    string generatedResponse = "";
                                    generatedResponse += "{\"result\" : " + result + "}";
                                    data = Encoding.UTF8.GetBytes(generatedResponse);
                                }
                            }
                            //Gets the total of an order
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
                            //Get the total of a specific user for the order
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
                                if (orderId != "-1" && user != "-1")
                                {
                                    double price = await GetUserPrice(user, orderId);
                                    price = Math.Round(price, 2);
                                    data = Encoding.UTF8.GetBytes("{ \"Result\": \"" + price.ToString() + "\"}");
                                }
                            }
                            //Check if user has admin rights
                            else if (values[0] == "CHECK_ADMIN")
                            {
                                bool result = await CheckAdmin(values[1]);
                                data = Encoding.UTF8.GetBytes("{\"Result\":\"" + result + "\"}");

                            }
                            
                            //Updates products with new ones ie replacing product with different one. the front end will call this then also a delete POST request to delete the old object, shouldnt probably do this this method should handle both aspects
                            else if (values[0] == "UPDATE_PRODUCT")
                            {
                                string query = "";
                                string id = "";
                                string orderId = "";
                                string username = "";
                                string word = "";
                                bool first = false;
                                bool second = false;
                                foreach (char c in values[1])
                                {
                                    if (c.ToString() == ",")
                                    {
                                        if (!first)
                                        {
                                            query = word;
                                            word = "";
                                            first = true;
                                        }
                                        else if (!second)
                                        {
                                            id = word;
                                            word = "";
                                            second = true;
                                        }
                                        else
                                        {
                                            orderId = word;
                                            word = "";
                                        }
                                    }
                                    else
                                    {
                                        word += c.ToString();
                                    }
                                }
                                username = word;
                                int result = await UpdateProduct(query, id, orderId, username);
                                data = Encoding.UTF8.GetBytes("{\"Result\":\"" + result + "\"}");

                            }

                            //Fetch Names of people with items in the order provided
                            else if (values[0] == "NAMES")
                            {
                                List<string> result = await FetchNames(values[1]);
                                if (result[0] != "-1")
                                {


                                    string final = "{\"Result\":[";
                                    bool first = true;
                                    foreach (string name in result)
                                    {
                                        if (first)
                                        {
                                            first = false;
                                        }
                                        else
                                        {
                                            final += ",";
                                        }
                                        final += "\"" + name + "\"";
                                    }
                                    final += "]}";


                                    data = Encoding.UTF8.GetBytes(final);
                                }
                            
                            }

                            //Add delivery fee to a specific user
                            else if (values[0] == "ADD_DELIVERY")
                            {
                                string orderId = "";
                                string name = "";
                                string price = "";
                                bool first = true;
                                string current = "";
                                for(int i = 0; i < values[1].Length; i++)
                                {
                                    if (values[1][i].ToString() == ",")
                                    {
                                        if (first)
                                        {
                                            first = false;
                                            orderId = current;
                                            current = "";
                                        }
                                        else
                                        {
                                            name = current;
                                            current = "";
                                        }
                                    }
                                    else
                                    {
                                        current += values[1][i].ToString();
                                    }

                                }
                                price = current;
                                if(!(orderId ==""|| name ==""|| price == ""))
                                {
                                    int result = -1;
                                    try
                                    {
                                        result = await AddDelivery(orderId, name, float.Parse(price));
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }
									data = Encoding.UTF8.GetBytes("{\"Result\":\"" + result + "\"}");
								}
                            }

                            //Add order to database with specified name
                            else if (values[0] == "ADD_ORDER")
                            {
                                int result = await AddOrder(values[1]);
                                Console.WriteLine(result+" : " + values[1]);
                                data = Encoding.UTF8.GetBytes("{\"Result\":\"" + result + "\"}");
							}
                        }
                    }
                    if (data == null)
                    {
                        data = Encoding.UTF8.GetBytes(String.Format("{{\"Data\": \"Error\"}}"));
                    }

                    //Sends response to the frontend
                    response.ContentType = "application/json";
                    response.ContentEncoding = Encoding.UTF8;
                    response.ContentLength64 = data.LongLength;
                    await response.OutputStream.WriteAsync(data, 0, data.Length);
                    response.Close();
                }
            }
        }

		/// <summary>
		/// Task <c>CreateAccount</c> Checks if the account can be created ie username not taken then calls SQLCreateAccount
		/// </summary>
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

		/// <summary>
		/// Method <c>PostRequest</c> Makes a POST request to tesco using the <paramref name="itemName"/> to search for that item as if you were on their website
		/// </summary>
		public static List<List<string>>PostRequest(string itemName)
        {
            //Replicating the request that would be sent if you searched on the tesco website but adds the itemName passed from our tesco frontend
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

		/// <summary>
		/// Method <c>ExtractData</c> The old way of extracting the products from the search result performed by PostRequest. This method doesnt use Newtonsoft libary but is less effecient
		/// </summary>
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

		/// <summary>
		/// Method <c>ExtractDataV2</c> The new way of extracting the products from the search result performed by PostRequest. This method does use Newtonsoft libary and is more effecient
		/// </summary>
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
                            if (!startPrice)
                            {
                                string tempPrice = firstThree;
                                string tempPrice2 = "";
                                for(int i = 0; i < tempPrice.Length-1; i++)
                                {
                                    tempPrice2 += tempPrice[i].ToString();
                                }
                                final = "0." + tempPrice2;
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

		/// <summary>
		/// Method <c>SQLCreateAccount</c> Passes username and password to Create Method and returns 0 or -2 depending on outcome
		/// </summary>
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

		/// <summary>
		/// Method <c>Create</c> Adds the user to the database
		/// </summary>
		public static async void Create(string username,string password)
        {
			DocumentReference docRef = db.Collection("users").Document();
			Dictionary<string, object> user = new Dictionary<string, object>
			{
				{ "username", username },
				{ "password", password },
				{ "orderId","null" },
                {"real",true },
                {"admin",false }
			};
			await docRef.SetAsync(user);
		}

		/// <summary>
		/// Method <c>SQLCheckAccount</c> Checks if the username is already associated to an account
		/// </summary>
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

		/// <summary>
		/// Task <c>Login</c> Logs the user in if they have the correct username and password and returns their userid username and if they are admin or not. THIS ISNT A SECURE WAY 
		/// </summary>
		public static async Task<List<object>> Login(string username,string password)
		{
			bool correct = false;
            string id = null;
            bool admin = false;
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
                       // Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
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
                        if(pair.Key == "admin")
                        {
							if(pair.Value.ToString() == "True")
                            {
								admin = true;
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
            //Returns -2 orm -1 which will be displayed as different error on the front end
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
                temp4.Add(admin);
				return temp4;
			}
		}


		/// <summary>
		/// Method <c>PutRequest</c> Adds the product to the basket on the real tesco website
		/// </summary>
		public static int PutRequest(string productId,string itemName,int quantity,List<string>parameters)
        {
            //Headers required for the request
            // x-csrf-token, cookie, Cookie, change with each login to tesco website so every time you want to add items to basket login to your order on tesco website select the order and add an item to the basket, then copy the values in that request
            //and replace them in this code then will work
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://www.tesco.com/groceries/en-GB/trolley/items?_method=PUT");
            httpWebRequest.Method = "PUT";
            httpWebRequest.Headers["Cookie"] = "atrc=7018fe14-25aa-4b69-afb1-deec4dad0e2a; h-e=ec5a7f09a46b825f424c83ecad5ee4ff4ef3f09b188a7cb7f6719ff50f07a0b2; trkid=ed990236-5fb4-4166-ae7e-9ead9a6801b8; _abck=DB3EA9072434AD7B6985E6052305F9F6~-1~YAAQFDZ6XExyJpWGAQAAUXtbmQmWoWVpk/0V7JdX56KquHbCXAs+20daCpGLI1a7TXcOYuXPY2xCeDBBYEzadLx7n5tmwIgmeqRCWK1Jo6V9GHq8N9P1rZOMCjgkk9Erk9HOm7iZzeN+w6Q3iXnnKEAujAVi4gxrATYwqRJM1suX9g6MbMe5GjoQQ3iBII8pNknIl6gABPjfShrGUe/Ks2TZaTWzPnJpnQlxt75jsXAPWKRnHWQZwTGZfKCEEemTF/WcxCAg7dVDMmhF9SIBrH9U3mp36rEMObXZxYCiumRNciTDFekjPYKDnXGvUYBE1eAiOBffx32socbcs7/ZozBP+aIB9zh+daPtSSJMF5aQZsUMIS+P5pftHfYXdhAUwDkLqFVfBNQaNTVGbvyKYzP9BbPIYhU=~0~-1~1677612461; atrc=7018fe14-25aa-4b69-afb1-deec4dad0e2a; _csrf=Ag15oY6ER-yN2zw-Vpz4xdzMnUSnS3Y9QQzjw";
			//shift home
			httpWebRequest.Headers["authority"] = "www.tesco.com";
            httpWebRequest.Accept = "application/json";
            httpWebRequest.Headers["accept-language"] = "en-GB,en-US;q=0.9,en;q=0.8";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Headers["cookie"] = "null; null; null; consumer=default; trkid=ed990236-5fb4-4166-ae7e-9ead9a6801b8; atrc=7018fe14-25aa-4b69-afb1-deec4dad0e2a; DCO=sdc; h-e=ec5a7f09a46b825f424c83ecad5ee4ff4ef3f09b188a7cb7f6719ff50f07a0b2; deliveryPreferences=j%3A%7B%22addressId%22%3A%22104610347%22%7D; ighs-sess=eyJzdG9yZUlkIjoiNTkzMCIsImFuYWx5dGljc1Nlc3Npb25JZCI6IjI4MjAzZjdjZDAwN2VkYzE4ZDEzOTgwNjBkOTZmODNhIiwidXVpZCI6IjUyZjJlNmI0LTM5MDctNDZiMi1hNjdkLWEzMDIzMzM3ODRmZSIsInJlcXVlc3RCYWNrdXBzIjpbeyJpZCI6IjAxYTJlOTI4LWIxZGMtNDJlYy1iZjIzLWU2M2M0YjAxNWQ1YSIsIm1ldGhvZCI6IlBVVCIsInVybCI6Ii9ncm9jZXJpZXMvZW4tR0IvdHJvbGxleS9pdGVtcz9fbWV0aG9kPVBVVCIsImJvZHkiOnsiaXRlbXMiOlt7ImlkIjoiMzA4NDkzODgzIiwibmV3VW5pdENob2ljZSI6InBjcyIsIm5ld1ZhbHVlIjowLCJvbGRVbml0Q2hvaWNlIjoicGNzIiwib2xkVmFsdWUiOjEsInN1YnN0aXR1dGlvbk9wdGlvbiI6IkZpbmRTdWl0YWJsZUFsdGVybmF0aXZlIn1dLCJyZXR1cm5VcmwiOiIvZ3JvY2VyaWVzL2VuLUdCL3NlYXJjaD9xdWVyeT1waXp6YSJ9LCJ0aW1lc3RhbXAiOjE2ODU4MjM1Mzg1NzJ9XX0=; ighs-sess.sig=dI-3mTyt8ikQ7WMirKlt0VJst50; null; itemsPerPage=48; _csrf=rvuvUUpGwD_0zLd27YIu1QfA; cookiePreferences=%7B%22experience%22%3Atrue%2C%22advertising%22%3Atrue%7D; AMCVS_E4860C0F53CE56C40A490D45%40AdobeOrg=1; s_cc=true; _mibhv=anon-1670580767025-9014973469_4481; _4c_mc_=bb336543-3d61-4324-93b6-d9db09c3b08b; optimizelyEndUserId=oeu1670946505908r0.7781257664873842; CID=109635388; HYF=1; _gcl_au=1.1.19915024.1678654731; _gac_UA-17489144-30=1.1682800008.EAIaIQobChMI8Yj7h_bP_gIV0O7tCh3-EwiaEAAYASAAEgI4u_D_BwE; _gcl_aw=GCL.1682800008.EAIaIQobChMI8Yj7h_bP_gIV0O7tCh3-EwiaEAAYASAAEgI4u_D_BwE; atrc=7018fe14-25aa-4b69-afb1-deec4dad0e2a; UUID=52f2e6b4-3907-46b2-a67d-a302333784fe; bm_sz=3852E10D3991C14D54707462B9C6C4D3~YAAQNjZ6XHJrcG6IAQAA/wXpghM6TFJGetwhVabzjZ9F4E0dEM/pW7LWYRcmrxrKL8VdcieY37jvny9WM2YGDoPoOBrTJTdMVlD5zcwIW20aNVjLtN1/8EnAgjq7oAaE+QGmv2uuDVNTD74Dc8YWCpCHKKmlOrvQlQNYeVkMlEPP5u0kQd060szNmpwooQLk1RfhX79gR9StGG1i0tP+lMjR01UlZ145RNOjtwQLVsVwD16PlV+PKafJPOzVW1gVMnfujUBWb23+FgGpnSPis40w6fW0TN75hRJqvyKwrSki1g==~4342320~3749940; AKA_A2=A; AMCV_E4860C0F53CE56C40A490D45%40AdobeOrg=1585540135%7CMCIDTS%7C19511%7CMCMID%7C89858544417324752138609249372332897292%7CMCAID%7CNONE%7CMCOPTOUT-1685830693s%7CNONE%7CvVersion%7C4.4.0%7CMCAAMLH-1686428293%7C6%7CMCAAMB-1686428293%7Cj8Odv6LonN4r3an7LhD3WZrU1bUpAkFkkiY1ncBR96t2PTI; s_nr=1685823495965-Repeat; OAuth.AccessToken=eyJraWQiOiIyMTM1MGE2NC02YTYxLTRiNmYtYjIyZS1lYTNkOTNiY2YyMTQiLCJhbGciOiJSUzI1NiJ9.eyJqdGkiOiI5YTk3MmQwYS01MmI4LTRjYzUtOGM2Yy02MjIzMDNkNzc0Y2MiLCJpc3MiOiJodHRwczovL2FwaS50ZXNjby5jb20vaWRlbnRpdHkvdjQvaXNzdWUtdG9rZW4iLCJzdWIiOiI1MmYyZTZiNC0zOTA3LTQ2YjItYTY3ZC1hMzAyMzMzNzg0ZmUiLCJpYXQiOjE2ODU4MjM0OTcsIm5iZiI6MTY4NTgyMzQ5NywiZXhwIjoxNjg1ODI3MDk3LCJzY29wZSI6ImludGVybmFsIHB1YmxpYyIsImNvbmZpZGVuY2VfbGV2ZWwiOjEyLCJjbGllbnRfaWQiOiJhMTcxNDcwZS1iMGUzLTQ2ZDEtYmYxMi1jMjBiNDEyOGY0YjYiLCJ0b2tlbl90eXBlIjoiYmVhcmVyIiwic2lkIjoiMDFIMjFFSjk0UEQ2Q0VEUUozUThNTUZYSDEtY2MwYzgxOWMtZmExYy00NTE1LTg4YjktNWI4ZjE1ZTQ5MzU3LXRVTkE3eFphTGRpWVF4RlNVRTZBMHA2WWhnZlpLRUI4Rm9jZSJ9.AaCcsdPpheEVGsJAniLvRrD9vefMaKoTFzWSPcXRvg1vXlxYXIRS67On2kQcsm5lRjqKZkJsW8bytIj_0obsLd7ZLyFtowC01gVHiELgIWay_Mt9HkszWZ1anagUlaDgK9KXOKOQoz_zpe6B0H5jWY-OpEboGno3sdR_buBf93_Os2QjvmSBPl9Vw3XifvA1ZpjBcdzOuT14CrV3SUa1zyNp2XXTDfpieWWP7a1N2KQGA-WWthzzlQsoTCfOrFbBO5gMcCLGCKnGpL3NOyKiIfgJ5xSCJMrzQDzVe75t-0ApDExDofCRXzs2_3wBAH_FukLpC77iHD0oroSJ0LF7hg; OAuth.RefreshToken=D0101H21EJ94PD6CEDQJ3Q8MMFXH2mA0X3iM; OAuth.TokensExpiryTime=%7B%22AccessToken%22%3A1685827096519%2C%22RefreshToken%22%3A1685830697519%7D; akavpau_OneAccount=1685823797~id=fa4a15117b1ef712061acb15849a6b29; bm_mi=28B316111B6DE1C09A21522AA40E86F4~YAAQLjZ6XLwz80CIAQAAFCzpghP6BP6rH2hp6KEUYuvPRL6gdmJ3ihz2cz2sSpj1V/2QZkoOMDfDCxkOylR3IsgCdPoBDhL12KmPL51vPfjDlNp5EwS6R+QAKDwACixn6J8QnJeH6DrilwJP1/0pB8j2IrVwGhgJSPcO/F+WuqlglJ8kq5G0cfnO/u9SQB69c+QtzVnf/CetP3p9BdglMl5g5cVlIv+MWn8n4jamahNPLWCbzpuCkbWLhaRPnb996PDZOCTRIAgFAd85cX4fAREyF6rW7wZvpQv6EPmQknEM9escwGNO66pNCYEU7TX7irxqhn3QNRcwBS9FD7acUfXzoA==~1; _gid=GA1.2.622698922.1685823503; _gat_ukLego=1; ak_bmsc=84E3C4645BC24DA8493E5C7BB0116AE7~000000000000000000000000000000~YAAQSzZ6XECbIneIAQAAQmbpghPpESEK8bmj6XZhFzY/zFBWxEILNMuNsQugKqkj4q844utKAkGgPfX3yO2kY0v1X3gMCyDN5cpr8HuJryfoIu1R2/Q96JutSrCVhE2eiMQtp4yciEOtMsDjrXDrjkMwCLT7g2aBc1wZFJtGTG9ucYXjF9kG8xhk6POg9hbdO/Zw6/yUUkgpsw79deriicwEbrn5jf9d5HDF5j7VOYagcva7K/4W7QjwB9k87k3X8lObIhA5lmfahD6R+TkQekoLOLCjuHZ81rSk4d86jkFcyo8rugisMd9bNtzVSyVYRYJ+gdgtMg6KtrpJnI4floYPYq4w4Y28a4jVoK2bOjRJ4bKLMsRuz7qsPT+UJRjLDkArKvdyFlFuOjFte/2KYmWVyG0aZmEe7LLdM8XoxxgtgFwK3l1uglsnhR7eGO52; da_sid=905CBC1C8E33AE8C5368AA13B398CABAD6|4|0|4; da_lid=90FC84D19A73EA10B0A5BB99F5B6BD09F6|0|0|0; da_intState=; _ga=GA1.2.725422933.1670716073; _abck=DB3EA9072434AD7B6985E6052305F9F6~0~YAAQODZ6XNrimG2IAQAAYIzpgglvhvIol3iJsXR7rpu9UO5CeXl3UA7uzSQvbyvZxf96Gnr2zbGInOits8tYeLCUinV+7x+WvjtVsxNCF17dC3NCphdCHg2htTl/2dDIphpjiD1hBVIHNzx0SFKU88X1Ln3RqqKZLiYXG1i2GNxcBGxRXGo2dhvtpknu60dZfmL5Gakr4lEHG8kWr9xL0Y+rl11g5sIqgnn7w813ucBsCyRATqReUD6LV5kKhSFbyB9C5FpgRauAgh9sjbreVhlgTIvTQ/tm5F0/icsu9jBiKLuvPfv1u1CkiiNL23P6lFYgNBVHRrL4lym2kUJnN2TBx7RQzlmoP0dxC/gvNWUS/2qcX3qg2zZpmlELtWrvJAodDCK+zXKUnHefu6JWuWpIgomhDds=~-1~-1~1685827011; s_gpv_pn=search%20results; _uetsid=c9f232a0024b11ee8814f9ac39cd959b; _uetvid=0724a1b077aa11ed9f1a3321eb13f478; akavpau_tesco_groceries=1685823840~id=fa958ca436255a3658e3dc889f1714c0; bm_sv=796364C9CD9642E28C316648A83F1F8C~YAAQKjZ6XN8Qc3WIAQAA6c3pghOaW5u5rR7y1W3YkiqLJfwFGyvjN7SsBEWiPaC6cI9U+6S01Yo1j1vLOxNgQlHJjG6Xn733hp1xYLLy3/zPwbS2iInHEtE9N9/2pXqvPKYAr2+CUQpgjDgBB4M/VK9IWRLC84Qotv+TDMJY7wdzVyNxkdtXhWZN7uGCsOJAgzyH9wDr5HSI0sfGinILXyZA+TrhGz3aPuV6ljK9nOgOq9q41kbwXkF6Z0z1VtLC~1; _ga_33B19D36CY=GS1.1.1685823491.38.1.1685823540.11.0.0";
            httpWebRequest.Headers["origin"] = "https://www.tesco.com";
            httpWebRequest.Referer = "https://www.tesco.com/groceries/en-GB/search?query=" + itemName;
           // httpWebRequest.Headers["sec-ch-ua"] = parameters[2];
            httpWebRequest.Headers["sec-ch-ua-mobile"] = "?0";
            httpWebRequest.Headers["sec-ch-ua-platform"] = "\"Windows\"";
            httpWebRequest.Headers["sec-fetch-dest"] = "empty";
            httpWebRequest.Headers["sec-fetch-mode"] = "cors";
            httpWebRequest.Headers["sec-fetch-site"] = "same-origin";
           // httpWebRequest.Headers["traceparent"] = parameters[2];
           // httpWebRequest.Headers["tracestate"] = parameters[3];
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";
            httpWebRequest.Headers["x-csrf-token"] = "QJfS5ijR-wiul6pxYCAA3OZLoULqIV9BZxyU";

			httpWebRequest.Headers["x-requested-with"] = "XMLHttpRequest";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.Headers["Accept-Encoding"] = "gzip, deflate, br";
            httpWebRequest.Headers["x-requested-with"] = "XMLHttpRequest";
            httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"items\":[{\"id\":\""+productId+"\",\"newValue\":"+quantity.ToString()+",\"oldValue\":0,\"newUnitChoice\":\"pcs\",\"oldUnitChoice\":\"pcs\",\"substitutionOption\":\"FindSuitableAlternative\"}],\"returnUrl\":\"/groceries/en-GB/search?query=" + itemName + "\"}";
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

		/// <summary>
		/// Task <c>GetPrice</c> Gets the total cost of an order
		/// </summary>
		public static async Task<string> GetPrice(string orderId)
        {
            string total = "";
			Query ordersQuery = db.Collection("orders");
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

		/// <summary>
		/// Task <c>GetDate</c> Gets the date of an order
		/// </summary>
		public static async Task<string> GetDate(string orderId)
        {
			string total = "";
            try
            {
                Query ordersQuery = db.Collection("orders");
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
            }
            catch (Exception)
            {
                return "-1";
            }
			return total;
		}

		/// <summary>
		/// Task <c>GetLive</c> Checks if a order is live or not
		/// </summary>
		public static async Task<bool> GetLive(string orderId)
        {
            bool live = false;
            Query ordersQuery = db.Collection("orders");
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
                        if (pair.Key == "live")
                        {
                            live = bool.Parse(pair.Value.ToString());

                        }


                    }
                }
            }
            return live;
        }

		/// <summary>
		/// Task <c>AddToOrderV2</c> Adds a product for the <paramref name="userId"/> to the order <paramref name="orderId"/>
		/// </summary>
		public static async Task<int> AddToOrderV2(int productId,float cost, string orderId, string userId, string query,string name,string img)
        {
            try
            {
                double price = double.Parse((await GetPrice(orderId)));
                bool live = await GetLive(orderId);
                if (!live)
                {
                    return -2;
                }
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
                //Deletes the products then adds the products again with the new product to be added as well, as when you update the field it merges similar products when you need them to be sepearate
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

		/// <summary>
		/// Task <c>GetProducts</c> Returns all the products from a order <paramref name="orderId"/>
		/// </summary>
		public async static Task<List<object>> GetProducts(string orderId)
        {
            List<object> products = new List<object>();
            try
            {
                Query ordersQuery = db.Collection("orders");
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
            //To remove null product
            for(int i = 0; i < products.Count; i++)
            {
                object prod = products[i];
                if(prod.ToString() == "null" || prod == null)
                {
                    products.RemoveAt(i);
                }
            }
            

            
            return products;
            
        }

		/// <summary>
		/// Task <c>UpdateProducts</c> Sets the field "added" on all products to True
		/// </summary>
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

                //We delete and add again because of same reason as the addToOrderV2 method
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

		/// <summary>
		/// Task <c>AddToCart</c> Adds all products on an order <paramref name="orderId"/> to the real tesco website cart
		/// </summary>
		public async static Task<int> AddToCart(string orderId,List<string> parameters)
        {
            List<object> products = await GetProducts(orderId);
            if (products[0] == "null")
            {
                return -1;
            }
            List<object> addedProducts = new List<object>();
            Dictionary<string, int> toAdd = new Dictionary<string, int>();
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
                    //Checks if the added Field is false so it needs to be added to tesco cart       
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
                //if product needs to be added to tesco cart
                if (add)
                {
                    addedProducts.Add(product);
                    if (toAdd.ContainsKey(id))
                    {
                        toAdd[id]++;
                    }
                    else
                    {
                        toAdd.Add(id, 1);
                    }
                    
                }
            }
            //makes a put request for all products that need to be added to the real tesco website cart
            foreach(string key in toAdd.Keys)
            {
				int result = PutRequest(key, "null",toAdd[key],parameters);
				if (result == -1)
				{
					return -3;
				}
			}
			//Set all the added fields for all products to true
			return await UpdateProducts(addedProducts, orderId);
        }

		/// <summary>
		/// Task <c>AddOrder</c> adds an order to the database with the date <paramref name="date"/>
		/// </summary>
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
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }
            return 0;
        }


		/// <summary>
		/// Task <c>FetchOrders</c> Fetches all orders from the database
		/// </summary>
		public async static Task<List<List<string>>> FetchOrders()
        {
			List<List<string>> orders = new List<List<string>>();
			try
            {
                
                Query ordersQuery = db.Collection("orders");
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

		/// <summary>
		/// Task <c>FetchProducts</c> Fetches all products with the orderId <paramref name="orderId"/> if a filtered name <paramref name="name"/> is provided then returns all products matching the name
		/// </summary>
		public async static Task<List<List<string>>> FetchProducts(string orderId,string name = "all")
        {
            bool filtered = false;
            if(name != "all")
            {
                filtered = true;
            }
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
                string tempName = "";
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
                        tempName = fields.Value.ToString();
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
                if (filtered)
                {
                    // if filtered is true and name doesnt match with the filtered name then remove product 
                    Console.WriteLine("NAME: " + name + "  tempName: " + tempName);
                    if(tempName != name)
                    {
                        productsList.RemoveAt(productsList.Count - 1);
                        queryList.RemoveAt(queryList.Count - 1);
                        userList.RemoveAt(userList.Count - 1);
                        imageList.RemoveAt(imageList.Count - 1);
                        nameList.RemoveAt(nameList.Count - 1);
                        priceList.RemoveAt(priceList.Count - 1);
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

		/// <summary>
		/// Task <c>RemoveProduct</c> removes a product from an order as long as the userId <paramref name="userId"/> is the owner of the product
		/// </summary>
		public async static Task<int> RemoveProduct(float price, int productId, string orderId, string userId)
        {
            List<object> products = await GetProducts(orderId);
            double totalPrice = double.Parse(await GetPrice(orderId));
            bool live = await GetLive(orderId);
            if (!live)
            {
                return -1;
            }
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

		/// <summary>
		/// Task <c>GetUserPrice</c> Fetchs the total from an order <paramref name="orderId"/> for the user <paramref name="userId"/>
		/// </summary>
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

		/// <summary>
		/// Task <c>CheckAdmin</c> checks if a user <paramref name="userId"/> is an admin
		/// </summary>
		public async static Task<bool> CheckAdmin(string userId)
        {
            Console.WriteLine(userId);
			Query capitalQuery = db.Collection("users").WhereEqualTo("real", true);
			QuerySnapshot capitalQuerySnapshot = await capitalQuery.GetSnapshotAsync();
			foreach (DocumentSnapshot documentSnapshot in capitalQuerySnapshot.Documents)
			{
				Dictionary<string, object> city = documentSnapshot.ToDictionary();
                bool user = false;
				foreach (KeyValuePair<string, object> pair in city)
				{
					//Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
					if (pair.Key == "username")
					{
						if (pair.Value.ToString() == userId)
						{
                            user = true;
						}
					}
                    if(pair.Key == "admin" && user)
                    {
						//Console.WriteLine("RIRSJUIESISI");
						if (pair.Value.ToString() == "True")
                        {
                            Console.WriteLine("RIRSJUIESISI2");
                            return true;
                        }
                        else
                        {
							Console.WriteLine("RIRSJUIESISI3: "+pair.Value.ToString());
							return false;
                        }
                    }
				}
			}
            return false;
			
		}

		/// <summary>
		/// Task <c>UpdateProduct</c> adds a product from just its productId <paramref name="productId"/> to order <paramref name="orderId"/>
		/// </summary>
		public async static Task<int> UpdateProduct(string query, string productId,string orderId,string userId)
        {
            List<List<string>> products = PostRequest(query);
            int index = 0;
            foreach (string id in products[3])
            {
                Console.WriteLine(id);
                if(id == productId)
                {
                    break;
                }
                index++;
            }
            if(index == products[3].Count)
            {
                return -1;
            }
            Console.WriteLine(index);
			float price = float.Parse(products[1][index]);
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
            string finalPrice2 = finalPrice;
			if (products[4][index] != "null" && products[4][index] != "null2")
			{
                finalPrice2 = products[4][index];

			}
			
			
            int result = await AddToOrderV2(Convert.ToInt32(products[3][index]),float.Parse(finalPrice2), orderId, userId, query, products[0][index], products[2][index]);
            return result;
        }


		/// <summary>
		/// Task <c>AddOrder</c> Fetches all the people who have an item on order <paramref name="orderId"/>
		/// </summary>
		public async static Task<List<string>> FetchNames(string orderId)
        {
			List<object> products = await GetProducts(orderId);
			double total = 0;
			Dictionary<string, double> people = new Dictionary<string, double>();
			if (products[0] == "null")
			{
				return new List<string> { "-1"};
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

						if (fields.Key == "owner")
						{
							owner = fields.Value.ToString();
						}
					}
					try
					{
						
						people[owner] += 0;
					}
					catch (Exception)
					{
						people.Add(owner, 0);
					}
				}
			}
			catch (Exception)
			{
				return new List<string> { "-1"};
			}
            List<string> final = new List<string>();
			foreach (string person in people.Keys)
			{
                final.Add(person);
			}
			return final;
		}


		/// <summary>
		/// Task <c>AddDelivery</c> adds delivery fees for user <paramref name="userId"/>
		/// </summary>
		public async static Task<int> AddDelivery(string orderId,string userId,float price)
        {
            if (price > 0)
            {
				//https:susididfidfdodoidfd used so no image is rendered on frontend
				return await AddToOrderV2(-1, price, orderId, userId, "n/a", "Delivery Fee", "https:susididfidfdodoidfd.com");
            }
            else
            {
				return await AddToOrderV2(-1, price, orderId, userId, "n/a", "Discount Fee", "https:susididfidfdodoidfd.com");
			}
		}
    }
}
