using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace LockLock.Controllers
{
    public class BookingRoomController : Controller
    {
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirestoreDb firestoreDb;
        public BookingRoomController()
        {
            string projectId;
            using (StreamReader r = new StreamReader(firebaseJSON))
            {
                string json = r.ReadToEnd();
                var myJObject = JObject.Parse(json);
                projectId = myJObject.SelectToken("project_id").Value<string>();
            }
            firestoreDb = FirestoreDb.Create(projectId);
        }

        public async Task<IActionResult> IndexAsync(string room)
        {
            string adminUID = await verifyAdminTokenAsync();
            if (adminUID != null)
            {
                List<BookingModel> bookingList = new List<BookingModel>();

                Query transactionQuery = firestoreDb.Collection("transaction").WhereEqualTo("roomID", room);
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
                        int lastIndex = timeLists.Count - 1;
                        int timeCompare = 0;
                        if (lastIndex >= 0)
                        {
                            timeCompare = DateTime.Compare(timeLists[timeLists.Count - 1], currentDate);
                        }

                        DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                        DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                        RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                        DocumentReference userReference = firestoreDb.Collection("users").Document(transactionData.userID);
                        DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                        UserModel userData = userSnapshot.ConvertTo<UserModel>();

                        BookingModel bookingItem = new BookingModel()
                        {
                            BookingID = transactionSnapshot.Id,
                            Name = roomData.objName,
                            Num = 1,
                            RoomName = roomData.name,
                            timeList = timeLists,
                            status = timeCompare > 0 ? "Complete" : transactionData.cancel ? "Cancel" : "Booking",
                            cancel = true,//timeCompare > 0 ? false : !transactionData.cancel,
                            name = userData.Firstname + " " + userData.Lastname,
                            userID = transactionData.userID
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

        public async Task<IActionResult> cancleAsync(string transactionID)
        {
            string adminUID = await verifyAdminTokenAsync();
            if (adminUID != null)
            {
                DocumentReference transactionReference = firestoreDb.Collection("transaction").Document(transactionID);
                DocumentSnapshot transactionSnapshot = await transactionReference.GetSnapshotAsync();
                TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                if (roomData.adminID == adminUID)
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
                    Console.WriteLine("adminID not macth");
                    return RedirectToAction(nameof(Index));
                }


            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }




        private async Task<string> verifyAdminTokenAsync()
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
                    return decodedToken.Uid;
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
    }
}