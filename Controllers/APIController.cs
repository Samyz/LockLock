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


namespace LockLock.Controllers
{
    public class APIController : Controller
    {
        private readonly ILogger<APIController> _logger;
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private string projectId;
        private FirestoreDb firestoreDb;

        private string[] rooms = { "HhJCxmYvz3PbhlelTeqm", "mJPKvyzMqzvO91tWZOYU", "ujDeZXlmtfO19cJaw9xz", "hc2hLRAwGTNakdpeuS0z", "BzWSgoSk9RAQNEIjwJlp" };
        public APIController(ILogger<APIController> logger)
        {
            _logger = logger;
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseJSON);
            projectId = "locklock-47b1d";
            firestoreDb = FirestoreDb.Create(projectId);
        }
        [HttpGet]
        public async Task<IActionResult> table([FromQuery(Name = "room")] int roomQueryString)
        {
            if (roomQueryString > 5 || roomQueryString < 1)
                return NotFound(Json("Room Error"));
            int roomNum = roomQueryString;
            Console.WriteLine("QueryString => " + roomNum);
            RoomModel Room = new RoomModel();
            Room.RoomID = rooms[roomNum == 0 ? 0 : roomNum - 1];
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
                return NotFound(Json("Room Error"));
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
            TableDataModel sendData = new TableDataModel();
            InTableModel[] tempTable = new InTableModel[9];
            sendData.time = timeLength;
            sendData.timeDate = timeRef.Ticks;
            for (int j = 0; j < 9; j++)
            {
                InTableModel temp = new InTableModel();
                temp.time = (j + 9).ToString() + ".00";
                temp.data = new uint[7];
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
                    temp.data[i] = tableData[i, j];
                }
                tempTable[j] = temp;
            }
            sendData.data = tempTable;

            Console.WriteLine("finish");

            return Ok(Json(sendData));
        }
    }
    public class TableDataModel
    {
        public InTableModel[] data { get; set; }
        public long timeDate { get; set; }
        public string time { get; set; }
    }

    public class InTableModel
    {
        public string time { get; set; }
        public uint[] data { get; set; }
    }
}