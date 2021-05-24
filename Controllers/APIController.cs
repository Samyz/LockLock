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
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authentication;
using Firebase.Auth;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace LockLock.Controllers
{
    public class APIController : Controller
    {
        private readonly ILogger<APIController> _logger;
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirestoreDb firestoreDb;

        private FirebaseAuthProvider auth;

        private string[] rooms = { "HhJCxmYvz3PbhlelTeqm", "mJPKvyzMqzvO91tWZOYU", "ujDeZXlmtfO19cJaw9xz", "hc2hLRAwGTNakdpeuS0z", "BzWSgoSk9RAQNEIjwJlp" };
        public APIController(ILogger<APIController> logger)
        {
            _logger = logger;
            string projectId;
            using (StreamReader r = new StreamReader(firebaseJSON))
            {
                string json = r.ReadToEnd();
                var myJObject = JObject.Parse(json);
                projectId = myJObject.SelectToken("project_id").Value<string>();
            }
            firestoreDb = FirestoreDb.Create(projectId);

            auth = new FirebaseAuthProvider(
                            new FirebaseConfig("AIzaSyDYMUB0qohsGyFfdHCFWyxfcwr84HC-WCU"));
        }
        [HttpGet]
        public IActionResult Index()
        {
            return Content("sad");
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

        [HttpGet]
        public async Task<IActionResult> table([FromQuery(Name = "room")] string roomQueryString)
        {
            // Console.WriteLine("QueryString => " + roomQueryString + rooms.Contains(roomQueryString));
            if (!rooms.Contains(roomQueryString))
                return NotFound("Room Error");
            // int roomNum = roomQueryString;
            // Console.WriteLine("QueryString => " + roomNum);
            RoomModel Room = new RoomModel();
            // Room.RoomID = rooms[roomNum == 0 ? 0 : roomNum - 1];
            Room.RoomID = roomQueryString;
            Console.WriteLine("RoomID => " + Room.RoomID);

            try
            {
                Console.WriteLine("yo");
                DocumentReference documentReference = firestoreDb.Collection("room").Document(Room.RoomID);
                Console.WriteLine("yo");
                DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                Console.WriteLine(documentSnapshot.Exists);

                RoomModel newRoom = documentSnapshot.ConvertTo<RoomModel>();
                newRoom.RoomID = Room.RoomID;
                Room = newRoom;
            }
            catch
            {
                Console.WriteLine("in catch");
                return NotFound("Room Error");
            }

            DateTime timeRef = DateTime.Now.Date;
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

            Query borrowQuery = firestoreDb.Collection("borrow").WhereGreaterThanOrEqualTo("time", timeNow).WhereLessThanOrEqualTo("time", timeEnd).WhereEqualTo("cancel", false).WhereEqualTo("otherGroup", false).WhereEqualTo("roomID", Room.RoomID);
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

            uint[,] tableData = new uint[7, 9];
            for (int j = 0; j < 9; j++)
            {
                for (int i = 0; i < 7; i++)
                {
                    tableData[i, j] = 0;
                }
            }

            foreach (BorrowModel i in listBorrow)
            {
                // Console.WriteLine(i.time.Subtract(timeRef).ToString());//.Split(".")[0]
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
                // Console.WriteLine(day + " " + hour + " " + dayNow + " " + hourNow);
                if (hour < 18 && hour >= 9)
                {
                    tableData[x, hour - 9] = tableData[x, hour - 9] + 1;
                }
            }
            List<List<uint>> list = new List<List<uint>>();
            TableDataModel sendData = new TableDataModel();
            // sendData.roomID = Room.RoomID;
            sendData.time = timeLength;
            // sendData.timeDate = timeRef.Ticks;
            for (int j = 0; j < 9; j++)
            {
                List<uint> inList = new List<uint>();
                for (int i = 0; i < 7; i++)
                {
                    if (i == 0 && j <= hourNow - 9)
                    {
                        tableData[i, j] = 0;
                    }
                    else if (Room.objNum - tableData[i, j] <= 0)
                    {
                        tableData[i, j] = 0;
                    }
                    else
                    {
                        tableData[i, j] = Room.objNum - tableData[i, j];
                    }
                    inList.Add(tableData[i, j]);
                }
                list.Add(inList);
            }
            sendData.data = list;

            Console.WriteLine("finish");

            return Ok(sendData);
        }

        [HttpPost]
        public async Task<IActionResult> createTransaction([FromBody] string roomID, [FromBody] DateTime startDateTime, [FromBody] int hourPeriod)
        {
            List<DateTime> timeList = new List<DateTime>();
            if (startDateTime.Minute != 0 && startDateTime.Second != 0)
                return NotFound("Date Error");
            for (int i = 0; i < hourPeriod; i++)
            {
                timeList.Add(startDateTime.AddHours(i));
            }

            RoomModel Room = new RoomModel();
            try
            {
                DocumentReference documentReference = firestoreDb.Collection("room").Document(roomID);
                DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                Console.WriteLine(documentSnapshot.Exists);

                RoomModel newRoom = documentSnapshot.ConvertTo<RoomModel>();
                newRoom.RoomID = roomID;
                Room = newRoom;
            }
            catch
            {
                return NotFound("RoomID Error");
            }

            bool isError = false;
            foreach (DateTime date in timeList)
            {
                Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("time", TimeZoneInfo.ConvertTimeToUtc(date)).WhereEqualTo("cancel", false).WhereEqualTo("otherGroup", false).WhereEqualTo("roomID", Room.RoomID);
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
            if (isError)
            {
                return NotFound("DataError");
            }

            // CollectionReference transactionCollection = firestoreDb.Collection("transaction");
            // TransactionModel newTransaction = new TransactionModel()
            // {
            //     roomID = roomID,
            //     timestamp = DateTime.UtcNow,
            //     userID = user.UserID,
            //     cancel = false
            // };

            // DocumentReference transactionDocument = await transactionCollection.AddAsync(newTransaction);
            // string transactionId = transactionDocument.Id;

            // foreach (DateTime date in timeList)
            // {
            //     CollectionReference borrowCollection = firestoreDb.Collection("borrow");
            //     BorrowModel newBorrow = new BorrowModel()
            //     {
            //         roomID = roomID,
            //         time = date,
            //         transactionID = transactionId,
            //         cancel = false,
            //         otherGroup = false
            //     };
            //     DocumentReference borrowDocument = await borrowCollection.AddAsync(newBorrow);
            //     Console.WriteLine(date);
            // }

            return Ok("OK");

        }

        public async Task<IActionResult> allRoom()
        {
            Query roomQuery = firestoreDb.Collection("room");
            QuerySnapshot roomQuerySnapshot = await roomQuery.GetSnapshotAsync();

            List<RoomModel> roomList = new List<RoomModel>();

            foreach (DocumentSnapshot value in roomQuerySnapshot)
            {
                RoomModel room = value.ConvertTo<RoomModel>();

                room.RoomID = value.Id;
                roomList.Add(room);
            }
            roomList = roomList.OrderBy(o => o.number).ToList();
            return Ok(roomList);
        }

        [HttpPost]
        public async Task<IActionResult> loginAsync([FromBody] loginRequest request)
        {
            try
            {
                var fbAuthLink = await auth.SignInWithEmailAndPasswordAsync(request.email, request.password);
                string token = fbAuthLink.FirebaseToken;
                //saving the token in a session variable
                if (token != null)
                {
                    return Ok(token);
                }
                else
                {
                    Console.Write("BadRequest");
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                Console.Write("BadRequest");
                return BadRequest();
            }
        }
        [HttpGet]
        public async Task<IActionResult> getAllAsync()
        {
            string token = await HttpContext.GetTokenAsync("Bearer", "access_token");
            if (token != null)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }

        }
        [HttpPost]
        public async Task<IActionResult> cancelTransaction([FromBody] cancelRequest request)
        {
            try
            {
                DocumentReference transactionReference = firestoreDb.Collection("transaction").Document(request.id);
                DocumentSnapshot transactionSnapshot = await transactionReference.GetSnapshotAsync();

                TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                if (true)
                {
                    await transactionReference.UpdateAsync("cancel", true);

                    Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                    QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                    foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                    {
                        DocumentReference borrowReference = firestoreDb.Collection("borrow").Document(borrowSnapshot.Id);
                        await borrowReference.UpdateAsync("cancel", true);
                    }
                    return Ok();
                }
                else
                {
                    Console.WriteLine("UserID not macth");
                    return BadRequest();
                }

            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]

        public async Task<IActionResult> getTransectionByUserAsync()
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
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
                            cancel = timeCompare > 0 ? true : false
                        };
                        bookingList.Add(bookingItem);
                    }
                    else
                    {
                        Console.WriteLine("Document does not exist!", transactionSnapshot.Id);
                    }
                }
                return View(bookingList);
            }
            else
            {
                return BadRequest();
            }
        }


        private async Task<string> verifyTokenAsync()
        {
            try
            {
                string authorizationHeader = this.HttpContext.Request.Headers["Authorization"];
                string token = authorizationHeader.Substring(7);
                FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                return decodedToken.Uid;
            }
            catch
            {
                Console.WriteLine("ID token must not be null or empty");
                return null;
            }
        }




    }
    public class TableDataModel
    {
        public List<List<uint>> data { get; set; }
        public string time { get; set; }
        // public string roomID { get; set; }
    }

    public class loginRequest
    {
        [Required]
        [JsonPropertyName("email")]
        public string email { get; set; }
        [Required]
        [JsonPropertyName("password")]
        public string password { get; set; }
    }
    public class cancelRequest
    {
        [Required]
        [JsonPropertyName("id")]
        public string id { get; set; }

    }


}