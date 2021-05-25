using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using LockLock.Models;
using Newtonsoft.Json;
using FirebaseAdmin.Auth;

using Newtonsoft.Json.Converters;

using Nancy.Json;
using System.IO;
using System.Net;


namespace LockLock.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private string projectId;
        private FirestoreDb firestoreDb;

        private string[] rooms = { "HhJCxmYvz3PbhlelTeqm", "mJPKvyzMqzvO91tWZOYU", "ujDeZXlmtfO19cJaw9xz", "hc2hLRAwGTNakdpeuS0z", "BzWSgoSk9RAQNEIjwJlp" };



        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            firestoreDb = FirestoreDb.Create("locklock-47b1d");
        }

        private async Task<string> checkLogedIn()
        {
            var token = HttpContext.Session.GetString("_UserToken");
            if (token != null)
            {
                try
                {
                    FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    Console.WriteLine(decodedToken.Uid);
                    return decodedToken.Uid;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return null;
            }
        }

        public async Task<IActionResult> Index([FromQuery(Name = "room")] int roomQueryString)
        {
            string uid = await checkLogedIn();
            UserModel user = new UserModel();
            if (uid != null)
            {
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine(documentSnapshot.Exists);
                    Console.WriteLine(uid);

                    UserModel newUser = documentSnapshot.ConvertTo<UserModel>();
                    newUser.UserID = uid;
                    user = newUser;
                    if (user.role == "admin")
                        return RedirectToAction("Index", "Admin");
                }
                catch
                {
                    return RedirectToAction("SignIn", "Account");
                }
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
            Console.WriteLine("UserID = " + user.UserID);
            Console.WriteLine("Firstname = " + user.Firstname);
            Console.WriteLine("Lastname = " + user.Lastname);
            Console.WriteLine("Email = " + user.Email);
            Console.WriteLine("Tel = " + user.Tel);

            BlacklistDataModel blacklist = new BlacklistDataModel();
            blacklist.adminID = "";
            try
            {
                Query blacklistQuery = firestoreDb.Collection("blacklist").WhereEqualTo("userID", user.UserID);
                QuerySnapshot blacklistQuerySnapshot = await blacklistQuery.GetSnapshotAsync();

                foreach (DocumentSnapshot documentSnapshot in blacklistQuerySnapshot.Documents)
                {
                    if (documentSnapshot.Exists)
                    {
                        BlacklistDataModel newBlacklist = documentSnapshot.ConvertTo<BlacklistDataModel>();
                        newBlacklist.BlacklistID = documentSnapshot.Id;
                        blacklist = newBlacklist;
                        Console.WriteLine(blacklist.adminID);
                    }
                    else
                    {
                        blacklist.adminID = "";
                        Console.WriteLine("else");
                    }
                }
            }
            catch
            {
                blacklist.adminID = "";
                Console.WriteLine("catch");
            }
            if (blacklist.adminID == "")
            {

                int roomNum = 0;
                roomNum = roomQueryString;
                Console.WriteLine("QueryString => " + roomNum);
                RoomModel Room = new RoomModel();
                Room.RoomID = rooms[roomNum == 0 ? 0 : roomNum - 1];
                Console.WriteLine("RoomID => " + Room.RoomID);

                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("room").Document(Room.RoomID);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine(documentSnapshot.Exists);

                    RoomModel newRoom = documentSnapshot.ConvertTo<RoomModel>();
                    newRoom.RoomID = Room.RoomID;
                    Room = newRoom;
                }
                catch
                {
                    return RedirectToAction(nameof(Error));
                }

                Console.WriteLine("RoomID = " + Room.RoomID);
                Console.WriteLine("adminID = " + Room.adminID);
                Console.WriteLine("name = " + Room.name);
                Console.WriteLine("objName = " + Room.objName);
                Console.WriteLine("objNum = " + Room.objNum);

                // get room data from Game API here //
                string token = await WebRequestLogin();
                List<GetRoomModel> list = await WebRequestGetAllRoom(token);
                List<List<int>> gameList = await WebRequestGetAllRoom(token, list[roomNum == 0 ? 0 : roomNum - 1].id);

                // data from our DB //
                TimeZoneInfo asiaThTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

                DateTime timeRef = DateTime.Now.Date;
                timeRef = TimeZoneInfo.ConvertTime(timeRef, asiaThTimeZone);

                DateTime timeNow = DateTime.Now.Date;
                timeNow = TimeZoneInfo.ConvertTimeToUtc(timeNow);
                // timeNow = TimeZoneInfo.ConvertTimeFromUtc(timeNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                DateTime timeEnd = timeNow.AddDays(7);
                Console.WriteLine("Now " + timeNow.ToString("u"));
                Console.WriteLine("Ref " + timeRef.ToString("u"));
                int hourNow = int.Parse(DateTime.Now.ToString("HH"));
                int dayNow = int.Parse(DateTime.Now.ToString("dd"));
                // Console.WriteLine("hour Now " + hourNow);
                string timeLength = timeRef.ToString("dd MMMM") + " - " + timeRef.AddDays(6).ToString("dd MMMM yyyy");
                List<string> viewDataName = new List<string>();
                for (int i = 0; i < 7; i++)
                {
                    viewDataName.Add(timeRef.AddDays(i).ToString("ddd"));
                }

                Query borrowQuery = firestoreDb.Collection("borrow").WhereGreaterThanOrEqualTo("time", timeNow).WhereLessThanOrEqualTo("time", timeEnd).WhereEqualTo("cancel", false).WhereEqualTo("otherGroup", null).WhereEqualTo("roomID", Room.RoomID);
                QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                List<BorrowModel> listBorrow = new List<BorrowModel>();

                foreach (DocumentSnapshot documentSnapshot in borrowQuerySnapshot.Documents)
                {
                    if (documentSnapshot.Exists)
                    {
                        Dictionary<string, object> borrow = documentSnapshot.ToDictionary();
                        string timeTemp = borrow["time"].ToString().Replace("Timestamp:", "").Trim();
                        borrow["time"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(timeTemp.Remove(timeTemp.Length - 1, 1), "s", null), TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

                        string json = JsonConvert.SerializeObject(borrow);
                        BorrowModel newBorrow = JsonConvert.DeserializeObject<BorrowModel>(json);
                        newBorrow.BorrowID = documentSnapshot.Id;
                        listBorrow.Add(newBorrow);
                    }
                }


                Tuple<string, uint>[,] tableData = new Tuple<string, uint>[7, 9];
                for (int j = 0; j < 9; j++)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        tableData[i, j] = new Tuple<string, uint>("", 0);
                    }
                }

                foreach (BorrowModel i in listBorrow)
                {
                    int day = int.Parse(i.time.ToString("dd"));
                    int hour = int.Parse(i.time.ToString("HH"));
                    int x;
                    if (day == dayNow)
                    {
                        x = 0;
                    }
                    else
                    {
                        x = int.Parse(i.time.Subtract(timeRef).ToString().Split(".")[0]);
                    }
                    if (hour < 18 && hour >= 9) // !(int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0]) == 0 && hour - hourNow <= 0) && 
                    {
                        tableData[x, hour - 9] = new Tuple<string, uint>(tableData[x, hour - 9].Item1, tableData[x, hour - 9].Item2 + 1);
                    }
                }

                for (int j = 0; j < 9; j++)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (i == 0 && j <= hourNow - 9)
                        {
                            tableData[i, j] = new Tuple<string, uint>("Grey", Room.objNum - tableData[i, j].Item2);
                        }
                        else if (Room.objNum - tableData[i, j].Item2 <= 0)
                        {
                            if (gameList[i][j] > 0)
                                tableData[i, j] = new Tuple<string, uint>("Yellow", (uint)gameList[i][j]);
                            else
                                tableData[i, j] = new Tuple<string, uint>("Red", 0);
                        }
                        else
                        {
                            tableData[i, j] = new Tuple<string, uint>("Green", Room.objNum - tableData[i, j].Item2);
                        }
                    }
                }

                TableModel viewData = new TableModel()
                {
                    objName = Room.objName,
                    timeLength = timeLength,
                    name = viewDataName,
                    table = tableData,
                    firstName = user.Firstname,
                    lastName = user.Lastname,
                    roomName = Room.name,
                    roomID = Room.RoomID,
                    roomNum = Array.IndexOf(rooms, Room.RoomID) + 1,
                    adminEmail = ""
                };

                return View(viewData);
            }
            else
            {
                UserModel admin = new UserModel();
                try
                {
                    Console.WriteLine(blacklist.adminID);
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(blacklist.adminID);
                    Console.WriteLine("news");
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine("newss");
                    Console.WriteLine(documentSnapshot.Exists);
                    Console.WriteLine(uid);

                    UserModel newUser = documentSnapshot.ConvertTo<UserModel>();
                    newUser.UserID = uid;
                    admin = newUser;
                }
                catch
                {
                    return RedirectToAction(nameof(Error));
                }
                int roomNum = 0;
                roomNum = roomQueryString;
                RoomModel Room = new RoomModel();
                Room.RoomID = rooms[roomNum == 0 ? 0 : roomNum - 1];
                TableModel viewData = new TableModel()
                {
                    adminEmail = admin.Email,
                    firstName = user.Firstname,
                    lastName = user.Lastname,
                    roomName = Room.name,
                    roomID = Room.RoomID,
                    roomNum = Array.IndexOf(rooms, Room.RoomID) + 1,
                };
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("room").Document(Room.RoomID);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine(documentSnapshot.Exists);

                    RoomModel newRoom = documentSnapshot.ConvertTo<RoomModel>();
                    newRoom.RoomID = Room.RoomID;
                    Room = newRoom;
                }
                catch
                {
                    return RedirectToAction(nameof(Error));
                }
                return View(viewData);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Transaction([FromBody] CreateTModel input)//public async Task<IActionResult> Transaction(string roomID, List<string> dates)
        {
            string uid = await checkLogedIn();
            UserModel user = new UserModel();
            if (uid != null)
            {
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();

                    UserModel newUser = documentSnapshot.ConvertTo<UserModel>();
                    newUser.UserID = uid;
                    user = newUser;
                }
                catch
                {
                    return StatusCode(400, "UserError");
                }
            }
            else
            {
                return StatusCode(400, "UserError");
            }

            Console.WriteLine();
            Console.WriteLine("UserID = " + user.UserID);
            Console.WriteLine("Firstname = " + user.Firstname);
            Console.WriteLine("Lastname = " + user.Lastname);
            Console.WriteLine("Email = " + user.Email);
            Console.WriteLine("Tel = " + user.Tel);
            // Console.WriteLine("RoomID = " + roomID);

            Console.WriteLine("RoomID = " + input.roomID);
            int dateCheck = -1;
            bool dateBool = false;
            foreach (string date in input.dates)
            {
                string[] temp = date.Split(" ");
                int tempI = int.Parse(temp[1]);
                if (dateCheck == -1)
                {
                    dateCheck = tempI;
                }
                else if (tempI != dateCheck)
                {
                    dateBool = true;
                }
                Console.WriteLine(date);
            }

            if (dateCheck == -1 || dateBool)
            {
                return StatusCode(400, "DataError");
            }
            //check data in Game API
            string token = await WebRequestLogin();
            List<GetRoomModel> list = await WebRequestGetAllRoom(token);
            List<List<int>> gameList = await WebRequestGetAllRoom(token, list[Array.IndexOf(rooms, input.roomID)].id);
            Console.WriteLine("GAME ID => " + list[Array.IndexOf(rooms, input.roomID)].id);

            // check data in DB
            RoomModel Room = new RoomModel();
            try
            {
                DocumentReference documentReference = firestoreDb.Collection("room").Document(input.roomID);
                DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                Console.WriteLine(documentSnapshot.Exists);

                RoomModel newRoom = documentSnapshot.ConvertTo<RoomModel>();
                newRoom.RoomID = input.roomID;
                Room = newRoom;
            }
            catch
            {
                return StatusCode(400, "DataError");
            }
            bool isError = false;
            DateTime timeNow = DateTime.Now.Date;
            timeNow = TimeZoneInfo.ConvertTimeToUtc(timeNow);
            for (int i = 0; i < input.dates.Count; i++)
            // foreach (string date in input.dates)
            {
                Console.WriteLine(input.color[i]);
                string[] temp = input.dates[i].Split(" ");
                string[] month = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

                TimeZoneInfo asiaThTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                DateTime timeCheck = new DateTime(int.Parse(temp[3]), Array.IndexOf(month, temp[2]) + 1, int.Parse(temp[1]), int.Parse(temp[4].Split(".")[0]), 0, 0);
                timeCheck = TimeZoneInfo.ConvertTime(timeCheck, asiaThTimeZone);
                if (input.color[i] == "Green")
                {
                    Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("time", TimeZoneInfo.ConvertTimeToUtc(timeCheck)).WhereEqualTo("cancel", false).WhereEqualTo("otherGroup", false).WhereEqualTo("roomID", Room.RoomID);
                    QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                    int count = 0;

                    foreach (DocumentSnapshot documentSnapshot in borrowQuerySnapshot.Documents)
                    {
                        if (documentSnapshot.Exists)
                        {
                            count++;
                        }
                    }
                    Console.WriteLine(count);
                    if (count >= Room.objNum)
                    {
                        isError = true;
                    }
                }
                else if (input.color[i] == "Yellow")
                {
                    TimeSpan interval = timeCheck - timeNow;
                    if (gameList[interval.Days][int.Parse(temp[4].Split(".")[0]) - 9] <= 0)
                    {
                        isError = true;
                    }
                }
            }
            if (isError)
            {
                return StatusCode(400, "DataError");
            }

            // store data in DB
            CollectionReference transactionCollection = firestoreDb.Collection("transaction");
            TransactionModel newTransaction = new TransactionModel()
            {
                roomID = input.roomID,
                timestamp = DateTime.UtcNow,
                userID = user.UserID,
                cancel = false
            };
            Console.WriteLine("Yahoo!");

            DocumentReference transactionDocument = await transactionCollection.AddAsync(newTransaction);
            string transactionId = transactionDocument.Id;

            // CollectionReference borrowCollection = firestoreDb.Collection("borrow");
            // BorrowModel newBorrow = new BorrowModel();

            Console.WriteLine(transactionId);

            for (int i = 0; i < input.dates.Count; i++)
            // foreach (string date in input.dates)
            {
                string[] temp = input.dates[i].Split(" ");
                string[] month = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

                TimeZoneInfo asiaThTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                DateTime save = new DateTime(int.Parse(temp[3]), Array.IndexOf(month, temp[2]) + 1, int.Parse(temp[1]), int.Parse(temp[4].Split(".")[0]), 0, 0);
                save = TimeZoneInfo.ConvertTime(save, asiaThTimeZone);


                if (input.color[i] == "Green")
                {
                    CollectionReference borrowCollection = firestoreDb.Collection("borrow");
                    BorrowModel newBorrow = new BorrowModel()
                    {
                        roomID = input.roomID,
                        time = TimeZoneInfo.ConvertTimeToUtc(save),
                        transactionID = transactionId,
                        cancel = false,
                        otherGroup = null
                    };
                    DocumentReference borrowDocument = await borrowCollection.AddAsync(newBorrow);
                    Console.WriteLine(input.dates[i]);
                    Console.WriteLine(TimeZoneInfo.ConvertTimeToUtc(save).ToString("u"));
                    Console.WriteLine(TimeZoneInfo.ConvertTimeToUtc(save));
                }
                else if (input.color[i] == "Yellow")
                {
                    string[] temporary = TimeZoneInfo.ConvertTimeToUtc(save).ToString("u").Split(" ");
                    string timeOut = temporary[0] + "T" + temporary[1].Substring(0, 8) + ".000Z";
                    Console.WriteLine("time out => " + timeOut);
                    GetCreateModel outs = await WebRequestCreate(token, list[Array.IndexOf(rooms, input.roomID)].id, timeOut);
                    if (outs == null)
                        return StatusCode(400, "DataError");

                    CollectionReference borrowCollection = firestoreDb.Collection("borrow");
                    BorrowModel newBorrow = new BorrowModel()
                    {
                        roomID = input.roomID,
                        time = TimeZoneInfo.ConvertTimeToUtc(save),
                        transactionID = transactionId,
                        cancel = false,
                        otherGroup = outs.id
                    };
                    DocumentReference borrowDocument = await borrowCollection.AddAsync(newBorrow);

                }
            }
            // return RedirectToAction("History", "Home");
            return Ok(Json("OK"));
        }

        public async Task<IActionResult> History()
        {
            string uid = await checkLogedIn();

            if (uid != null)
            {
                UserModel user = new UserModel();
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                    Console.WriteLine(documentSnapshot.Exists);
                    Console.WriteLine(uid);

                    UserModel newUser = documentSnapshot.ConvertTo<UserModel>();
                    newUser.UserID = uid;
                    user = newUser;
                    TempData["name"] = user.Firstname;
                    TempData["surname"] = user.Lastname;
                }
                catch
                {
                    return RedirectToAction("SignIn", "Account");
                }

                List<BookingModel> bookingList = new List<BookingModel>();

                Query transactionQuery = firestoreDb.Collection("transaction").WhereEqualTo("userID", uid).WhereEqualTo("cancel", false);
                QuerySnapshot transactionQuerySnapshot = await transactionQuery.GetSnapshotAsync();

                DateTime currentDate = DateTime.Now;

                foreach (DocumentSnapshot transactionSnapshot in transactionQuerySnapshot)
                {

                    if (transactionSnapshot.Exists)
                    {
                        TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                        Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                        QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                        List<DateTime> timeLists = new List<DateTime>();
                        foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                        {
                            BorrowModel borrowData = borrowSnapshot.ConvertTo<BorrowModel>();
                            timeLists.Add(borrowData.time.ToLocalTime());
                        }
                        timeLists.Sort();
                        int timeCompare = DateTime.Compare(timeLists[0].AddHours(-1), currentDate);

                        DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                        DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                        RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                        BookingModel bookingItem = new BookingModel()
                        {
                            BookingID = transactionSnapshot.Id,
                            Name = roomData.objName,
                            Num = 1,
                            RoomName = roomData.name,
                            timeList = timeLists,
                            cancel = timeCompare > 0 ? true : false,
                            timestamp = transactionData.timestamp
                        };
                        bookingList.Add(bookingItem);
                    }
                    else
                    {
                        Console.WriteLine("Document does not exist!", transactionSnapshot.Id);
                    }
                }
                bookingList = bookingList.OrderBy(x => x.timestamp).ToList();
                return View(bookingList);
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }


        public async Task<IActionResult> cancelAsync(string transactionID)
        {
            string uid = await checkLogedIn();
            string uidOther = await WebRequestLogin();
            if (uid != null)
            {
                DocumentReference transactionReference = firestoreDb.Collection("transaction").Document(transactionID);
                DocumentSnapshot transactionSnapshot = await transactionReference.GetSnapshotAsync();

                TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                if (transactionData.userID == uid)
                {
                    await transactionReference.UpdateAsync("cancel", true);

                    Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                    QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                    foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                    {
                        DocumentReference borrowReference = firestoreDb.Collection("borrow").Document(borrowSnapshot.Id);
                        BorrowModel borrowData = borrowSnapshot.ConvertTo<BorrowModel>();

                        if (borrowData.otherGroup != null)
                        {
                            cancelRequest(borrowSnapshot.Id, uidOther);
                        }

                        await borrowReference.UpdateAsync("cancel", true);
                    }
                    return RedirectToAction("History", "Home");
                }
                else
                {
                    Console.WriteLine("UserID not macth");
                    return RedirectToAction("History", "Home");
                }


            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }
        public async Task<IActionResult> Test()
        {
            string token = await WebRequestLogin();
            List<GetRoomModel> list = await WebRequestGetAllRoom(token);
            // string outs = await WebRequestCreate(token, 1);
            List<List<int>> test = await WebRequestGetAllRoom(token, 1);
            // Console.WriteLine(test[0][8]);
            return Ok(test);
        }
        private async Task<string> WebRequestLogin()
        {
            const string URL = "https://borrowingsystem.azurewebsites.net/api/user/login";
            try
            {
                var webRequest = System.Net.WebRequest.Create(URL);
                GetLoginModel login = new GetLoginModel();
                string json = new JavaScriptSerializer().Serialize(new
                {
                    email = "admin@locklock.com",
                    password = "password"
                });
                if (webRequest != null)
                {
                    webRequest.Method = "POST";
                    webRequest.Timeout = 12000;
                    webRequest.ContentType = "application/json";

                    await using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                    }

                    await using (System.IO.Stream s = webRequest.GetResponse().GetResponseStream())
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(s))
                        {
                            var jsonResponse = sr.ReadToEnd();
                            login = JsonConvert.DeserializeObject<GetLoginModel>(jsonResponse);
                        }
                    }
                }
                return login.accessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
        private async Task<List<GetRoomModel>> WebRequestGetAllRoom(string token)
        {
            const string URL = "https://borrowingsystem.azurewebsites.net/api/room/get-all";
            try
            {
                var webRequest = System.Net.WebRequest.Create(URL);
                List<GetRoomModel> list = new List<GetRoomModel>();
                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Timeout = 12000;
                    webRequest.Headers.Add("Authorization", "Bearer " + token);

                    await using (System.IO.Stream s = webRequest.GetResponse().GetResponseStream())
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(s))
                        {
                            var jsonResponse = sr.ReadToEnd();
                            list = JsonConvert.DeserializeObject<List<GetRoomModel>>(jsonResponse);
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
        private async Task<bool> cancelRequest(string transactionID, string token)
        {
            const string URL = "https://borrowingsystem.azurewebsites.net/api/reservation/delete";
            try
            {
                var webRequest = System.Net.WebRequest.Create(URL);
                string json = new JavaScriptSerializer().Serialize(new
                {
                    id = transactionID,
                });
                if (webRequest != null)
                {
                    webRequest.Method = "DELETE";
                    webRequest.Timeout = 12000;
                    webRequest.Headers.Add("Authorization", "Bearer " + token);
                    webRequest.ContentType = "application/json";
                    await using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                    }

                    HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                    Console.WriteLine((int)response.StatusCode);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        private async Task<GetCreateModel> WebRequestCreate(string token, int roomId, string time)
        {
            const string URL = "https://borrowingsystem.azurewebsites.net/api/reservation/create";
            try
            {
                var webRequest = System.Net.WebRequest.Create(URL);
                GetCreateModel model = new GetCreateModel();
                string json = new JavaScriptSerializer().Serialize(new
                {
                    roomId = roomId,
                    startDateTime = time,
                    hourPeriod = 1
                });
                if (webRequest != null)
                {
                    webRequest.Method = "POST";
                    webRequest.Timeout = 12000;
                    webRequest.Headers.Add("Authorization", "Bearer " + token);
                    webRequest.ContentType = "application/json";


                    await using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                    }
                    // HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                    // Console.WriteLine((int)response.StatusCode);
                    await using (System.IO.Stream s = webRequest.GetResponse().GetResponseStream())
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(s))
                        {
                            var jsonResponse = sr.ReadToEnd();
                            model = JsonConvert.DeserializeObject<GetCreateModel>(jsonResponse);
                        }
                    }
                }
                return model;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
        private async Task<List<List<int>>> WebRequestGetAllRoom(string token, int id)
        {
            const string URL = "https://borrowingsystem.azurewebsites.net/api/reservation/get-available-equipment-in-month";
            try
            {
                var webRequest = System.Net.WebRequest.Create(URL + "?Id=" + id.ToString());
                List<List<int>> list = new List<List<int>>();
                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Timeout = 12000;
                    webRequest.Headers.Add("Authorization", "Bearer " + token);

                    await using (System.IO.Stream s = webRequest.GetResponse().GetResponseStream())
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(s))
                        {
                            var jsonResponse = sr.ReadToEnd();
                            list = JsonConvert.DeserializeObject<List<List<int>>>(jsonResponse);
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
        public IActionResult Blacklist()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult EditProfile()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult SignIn()
        {
            return View();
        }

        public IActionResult MainRoom()
        {
            return View();
        }

        public IActionResult Edit()
        {
            return View();
        }

        public IActionResult HistoryAdmin()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    public class GetLoginModel
    {
        public string fullName { get; set; }
        public string email { get; set; }
        public string user { get; set; }
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public string profileImage { get; set; }
    }
    public class GetRoomModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string createBy { get; set; }
        public string dateModified { get; set; }
        public string equipmentName { get; set; }
    }
    public class GetCreateModel
    {
        public string id { get; set; }
        public int userId { get; set; }
        public int roomId { get; set; }
        public string startDateTime { get; set; }
        public string endDateTime { get; set; }
    }
}
