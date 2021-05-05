using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using LockLock.Models;
using Newtonsoft.Json;

using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using FirebaseAdmin.Auth;

namespace LockLock.Controllers
{
    public class UserController : Controller
    {
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirestoreDb firestoreDb;

        public UserController()
        {
            string projectId;
            // using (StreamReader r = new StreamReader(firebaseJSON))
            // {
            //     string json = r.ReadToEnd();
            //     var myJObject = JObject.Parse(json);
            //     projectId = myJObject.SelectToken("project_id").Value<string>();
            // }
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseJSON);
            projectId = "locklock-47b1d";
            firestoreDb = FirestoreDb.Create(projectId);
        }

        public async Task<IActionResult> Index()
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();

                    UserModel user = documentSnapshot.ConvertTo<UserModel>();
                    user.UserID = uid;
                    return View(user);
                }
                catch
                {
                    Console.Write("Exception : ");
                    Console.Write(ex);
                    return RedirectToAction("Index", "Home");
                }
<<<<<<< HEAD

                DocumentReference documentReference = firestoreDb.Collection("users").Document(decodedToken.Uid);
                DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();
                Console.WriteLine(documentSnapshot.Exists);
                Console.WriteLine(decodedToken.Uid);

                UserModel user = documentSnapshot.ConvertTo<UserModel>();
                Console.WriteLine(user.Email);
                user.UserID = decodedToken.Uid;

                return View(user);
=======
>>>>>>> dev
            }
            else
            {
                Console.WriteLine("ID token must not be null or empty");
                return RedirectToAction("SignIn", "Account");
            }

        }

        [HttpGet]
        public async Task<IActionResult> updateUser()
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();

                    UserModel user = documentSnapshot.ConvertTo<UserModel>();
                    return View(user);

                }
                catch (Exception ex)
                {
                    Console.Write("Exception : ");
                    Console.Write(ex);
                    return RedirectToAction(nameof(Index));
                }
            }
<<<<<<< HEAD
            catch
=======
            else
>>>>>>> dev
            {
                Console.WriteLine("ID token must not be null or empty");
                return RedirectToAction("SignIn", "Account");
            }
        }

        [HttpPost]
        public async Task<IActionResult> updateUser(UserModel user)
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                await documentReference.SetAsync(user, SetOptions.Overwrite);
                
                return RedirectToAction(nameof(Index));
            }
<<<<<<< HEAD
            catch
=======
            else
>>>>>>> dev
            {
                Console.WriteLine("ID token must not be null or empty");
                return RedirectToAction("SignIn", "Account");
            }
        }

        private async Task<string> verifyTokenAsync()
        {
            try
            {
                var token = HttpContext.Session.GetString("_UserToken");
                FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                return decodedToken.Uid;
            }
            catch
            {
                Console.Write("Exception : ");
                Console.WriteLine("ID token must not be null or empty");
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> getBorrow()
        {
            string roomID = "ROOM 1";
            Query roomQuery = firestoreDb.Collection("room").WhereEqualTo("name", roomID);
            QuerySnapshot roomQuerySnapshot = await roomQuery.GetSnapshotAsync();
            // List<RoomModel> listRoom = new List<RoomModel>();
            RoomModel Room = new RoomModel();

            foreach (DocumentSnapshot documentSnapshot in roomQuerySnapshot)
            {
                if (documentSnapshot.Exists)
                {
                    Dictionary<string, object> room = documentSnapshot.ToDictionary();
                    string json = JsonConvert.SerializeObject(room);
                    RoomModel newRoom = JsonConvert.DeserializeObject<RoomModel>(json);
                    newRoom.RoomID = documentSnapshot.Id;
                    Room = newRoom;
                    // listRoom.Add(newRoom);
                }
            }

            // RoomModel thisRoom = Room;//listRoom[0]
            Console.WriteLine("RoomID = " + Room.RoomID);
            Console.WriteLine("adminID = " + Room.adminID);
            Console.WriteLine("name = " + Room.name);
            Console.WriteLine("objName = " + Room.objName);
            Console.WriteLine("objNum = " + Room.objNum);
            // foreach (RoomModel i in listRoom)
            // {
            //     Console.WriteLine("RoomID = " + i.RoomID);
            //     Console.WriteLine("adminID = " + i.adminID);
            //     Console.WriteLine("name = " + i.name);
            //     Console.WriteLine("objName = " + i.objName);
            //     Console.WriteLine("objNum = " + i.objNum);
            // }

            // get room data from Game API here //
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
            List<string> viewDataName = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                viewDataName.Add(timeNow.AddDays(i).ToString("ddd"));
            }
            // foreach (string i in viewDataName)
            // {
            //     Console.WriteLine(i);
            // }
            // Console.WriteLine(timeNow.ToString("dd MMMM") + " - " + timeNow.AddDays(6).ToString("dd MMMM yyyy"));
            // Console.WriteLine(timeNow.AddDays(6).ToString("dd MMMM yyyy"));

            Query borrowQuery = firestoreDb.Collection("borrow").WhereGreaterThanOrEqualTo("time", timeNow).WhereLessThanOrEqualTo("time", timeEnd);
            QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
            List<BorrowModel> listBorrow = new List<BorrowModel>();

            foreach (DocumentSnapshot documentSnapshot in borrowQuerySnapshot.Documents)
            {
                if (documentSnapshot.Exists)
                {
                    Dictionary<string, object> borrow = documentSnapshot.ToDictionary();
                    string timeTemp = borrow["time"].ToString().Replace("Timestamp:", "").Trim();
                    borrow["time"] = TimeZoneInfo.ConvertTimeFromUtc(DateTime.ParseExact(timeTemp.Remove(timeTemp.Length - 1, 1), "s", null), TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                    // borrow["time"] = DateTime.ParseExact(timeTemp.Remove(timeTemp.Length - 1, 1), "s", null);
                    // Console.WriteLine(borrow["time"]);
                    // foreach (KeyValuePair<string, object> kvp in borrow)
                    // {
                    //     //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                    //     Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                    // }
                    string json = JsonConvert.SerializeObject(borrow);
                    // Console.WriteLine(json);
                    BorrowModel newBorrow = JsonConvert.DeserializeObject<BorrowModel>(json);
                    newBorrow.BorrowID = documentSnapshot.Id;
                    listBorrow.Add(newBorrow);
                }
            }
            foreach (BorrowModel i in listBorrow)
            {
                Console.WriteLine("BorrowID = " + i.BorrowID);
                Console.WriteLine("roomID = " + i.roomID);
                Console.WriteLine("time = " + i.time);
                // Console.WriteLine("userID = " + i.userID);
            }


            Tuple<string, uint>[,] tableData = new Tuple<string, uint>[7, 9];
            for (int j = 0; j < 9; j++)
            {
                for (int i = 0; i < 7; i++)
                {
                    tableData[i, j] = new Tuple<string, uint>("", 0);
                }
            }
            // tableData[1, 0] = new Tuple<string, uint>("Green", 1);
            for (int j = 0; j < 9; j++)
            {
                for (int i = 0; i < 7; i++)
                {
                    Console.Write(tableData[i, j].Item1 + "-" + tableData[i, j].Item2 + " ");
                }
                Console.WriteLine();
            }
            // List<List<Tuple<string, uint>>> viewDataTable = new List<List<Tuple<string, uint>>>();
            // List<Tuple<string, uint>> templateList = new List<Tuple<string, uint>>();
            // for (int i = 0; i < 9; i++)
            // {
            //     templateList.Add(new Tuple<string, uint>("", 0));
            // }
            // for (int i = 0; i < 7; i++)
            // {
            //     viewDataTable.Add(templateList);
            // }

            // Console.WriteLine(viewDataTable[1][0].Item1 + " " + viewDataTable[1][0].Item2);

            foreach (BorrowModel i in listBorrow)
            {
                Console.WriteLine(i.time.Subtract(timeRef).ToString());//.Split(".")[0]
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
                Console.WriteLine(day + " " + hour + " " + dayNow + " " + hourNow);
                if (hour < 18 && hour > 7) // !(int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0]) == 0 && hour - hourNow <= 0) && 
                {
                    // Console.WriteLine(viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])][hour - 9].Item1 + " " + viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])][hour - 9].Item2);

                    // List<Tuple<string, uint>> temp = viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])];
                    // for (int j = 0; j < 9; j++)
                    // {
                    //     Console.WriteLine(temp[j]);
                    // }
                    // temp[hour - 9] = new Tuple<string, uint>(viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])][hour - 9].Item1, viewDataTable[int.Parse(i.time.Subtract(timeNow).ToString().Split(".")[0])][hour - 9].Item2 + 1);
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
                        if (j % 2 == 0)
                            tableData[i, j] = new Tuple<string, uint>("Yellow", Room.objNum - tableData[i, j].Item2);
                        else
                            tableData[i, j] = new Tuple<string, uint>("Red", Room.objNum - tableData[i, j].Item2);
                    }
                    else
                    {
                        tableData[i, j] = new Tuple<string, uint>("Green", Room.objNum - tableData[i, j].Item2);
                    }
                }
            }

            for (int j = 0; j < 9; j++)
            {
                for (int i = 0; i < 7; i++)
                {
                    Console.Write(tableData[i, j].Item1 + "-" + tableData[i, j].Item2 + " ");
                }
                Console.WriteLine();
            }

            // for (int j = 0; j < 9; j++)
            // {
            //     for (int i = 0; i < 7; i++)
            //     {
            //         Console.Write(viewDataTable[i][j].Item1 + "-" + viewDataTable[i][j].Item2 + " ");
            //     }
            //     Console.WriteLine();
            // }

            TableModel viewData = new TableModel()
            {
                objName = Room.objName,
                timeLength = timeLength,
                name = viewDataName,
                table = tableData
            };

            // listBorrow.ForEach(Console.WriteLine);
            // Console.WriteLine(listBorrow);
            return View(viewData);
        }
    }
}