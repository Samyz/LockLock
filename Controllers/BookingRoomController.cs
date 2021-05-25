using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nancy.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LockLock.Controllers
{
    public class BookingRoomController : Controller
    {
        private FirestoreDb firestoreDb;
        public BookingRoomController()
        {
            firestoreDb = FirestoreDb.Create("locklock-47b1d");
        }

        public async Task<IActionResult> IndexAsync(string room)
        {
            Tuple<string, string, string> adminUID = await verifyAdminTokenAsync();
            string uidOther = await WebRequestLogin();
            if (adminUID != null)
            {
                TempData["name"] = adminUID.Item2;
                TempData["surname"] = adminUID.Item3;

                List<BookingModel> bookingList = new List<BookingModel>();

                Query transactionQuery = firestoreDb.Collection("transaction").WhereEqualTo("roomID", room);
                QuerySnapshot transactionQuerySnapshot = await transactionQuery.GetSnapshotAsync();

                DateTime currentDate = DateTime.Now;
                currentDate = currentDate.AddHours(7);

                foreach (DocumentSnapshot transactionSnapshot in transactionQuerySnapshot)
                {
                    if (transactionSnapshot.Exists)
                    {
                        TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                        Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                        QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                        List<DateTime> timeLists = new List<DateTime>();
                        bool checkCancel = false;
                        foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                        {
                            BorrowModel borrowData = borrowSnapshot.ConvertTo<BorrowModel>();
                            if (borrowData.otherGroup != null)
                            {
                                bool res = verifyReservationByID(uidOther, borrowData.otherGroup);
                                if (!res)
                                {
                                    checkCancel = true;
                                    Console.Write("reservation not found");
                                    break;
                                }
                            }
                            DateTime temp = borrowData.time;
                            timeLists.Add(temp.AddHours(7));
                        }
                        if (checkCancel && !transactionData.cancel)
                        {
                            DocumentReference transactionReference = firestoreDb.Collection("transaction").Document(transactionSnapshot.Id);
                            await transactionReference.UpdateAsync("cancel", true);

                            foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                            {
                                DocumentReference borrowReference = firestoreDb.Collection("borrow").Document(borrowSnapshot.Id);
                                await borrowReference.UpdateAsync("cancel", true);
                            }
                            continue;
                        }
                        timeLists.Sort();
                        int timeCompare = DateTime.Compare(timeLists[0].AddHours(-1), currentDate);

                        DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                        DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                        RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                        DocumentReference userReference = firestoreDb.Collection("users").Document(transactionData.userID);
                        DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                        UserModel userData = userSnapshot.ConvertTo<UserModel>();

                        Query blacklistQuery = firestoreDb.Collection("blacklist").WhereEqualTo("userID", userSnapshot.Id);
                        QuerySnapshot blacklistQuerySnapshot = await blacklistQuery.GetSnapshotAsync();

                        BookingModel bookingItem = new BookingModel()
                        {
                            BookingID = transactionSnapshot.Id,
                            Name = roomData.objName,
                            Num = 1,
                            RoomName = roomData.name,
                            timeList = timeLists,
                            status = timeCompare < 0 ? "Complete" : transactionData.cancel ? "Cancel" : "Booking",
                            cancel = timeCompare > 0 && !transactionData.cancel ? true : false,
                            inBlacklist = blacklistQuerySnapshot.Count == 0,
                            name = userData.Firstname + " " + userData.Lastname,
                            userID = transactionData.userID,
                            RoomID = roomSnapshot.Id
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
                return RedirectToAction("SignIn", "Account");
            }
        }

        public async Task<IActionResult> cancelAsync(string transactionID)
        {
            Tuple<string, string, string> adminUID = await verifyAdminTokenAsync();
            string uidOther = await WebRequestLogin();
            if (adminUID != null)
            {
                DocumentReference transactionReference = firestoreDb.Collection("transaction").Document(transactionID);
                DocumentSnapshot transactionSnapshot = await transactionReference.GetSnapshotAsync();
                TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                if (roomData.adminID == adminUID.Item1)
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
                            bool res = cancelRequest(borrowData.otherGroup, uidOther);
                            if (!res) Console.Write("Unable");
                        }
                        await borrowReference.UpdateAsync("cancel", true);
                    }
                    return RedirectToAction("Index", "BookingRoom", new { room = transactionData.roomID });
                }
                else
                {
                    Console.WriteLine("adminID not macth");
                    return RedirectToAction("SignIn", "Account");
                }
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }




        private async Task<Tuple<string, string, string>> verifyAdminTokenAsync()
        {
            try
            {
                var token = HttpContext.Session.GetString("_UserToken");
                FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                DocumentReference userReference = firestoreDb.Collection("users").Document(decodedToken.Uid);
                DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                UserModel user = userSnapshot.ConvertTo<UserModel>();

                if (user.role == "admin")
                {
                    return new Tuple<string, string, string>(decodedToken.Uid, user.Firstname, user.Lastname);
                }
                else
                {
                    Console.WriteLine("User role");
                    return null;
                }
            }
            catch
            {
                Console.WriteLine("ID token must not be null or empty");
                return null;
            }
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
        private bool verifyReservationByID(string token, string reservationID)
        {
            const string URL = "https://borrowingsystem.azurewebsites.net/api/reservation/get-reservation-by-id";
            try
            {
                var webRequest = System.Net.WebRequest.Create(URL + "?id=" + reservationID);

                if (webRequest != null)
                {
                    webRequest.Method = "GET";
                    webRequest.Timeout = 12000;
                    webRequest.Headers.Add("Authorization", "Bearer " + token);

                    HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                    if ((int)response.StatusCode >= 300) return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        private bool cancelRequest(string transactionID, string token)
        {
            const string URL = "https://borrowingsystem.azurewebsites.net/api/reservation/delete";
            try
            {
                var webRequest = System.Net.WebRequest.Create(URL + "?id=" + transactionID);

                if (webRequest != null)
                {
                    webRequest.Method = "DELETE";
                    webRequest.Timeout = 12000;
                    webRequest.Headers.Add("Authorization", "Bearer " + token);
                    webRequest.ContentType = "application/json";

                    HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                    Console.WriteLine((int)response.StatusCode);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}