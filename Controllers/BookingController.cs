using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LockLock.Controllers
{
    public class BookingController : Controller
    {
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirestoreDb firestoreDb;
        public BookingController()
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

        public async Task<IActionResult> IndexAsync()
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                List<BookingModel> bookingList = new List<BookingModel>();

                Query bookingQuery = firestoreDb.Collection("booking").WhereEqualTo("userID", uid);
                QuerySnapshot bookingQuerySnapshot = await bookingQuery.GetSnapshotAsync();

                foreach (DocumentSnapshot bookingSnapshot in bookingQuerySnapshot)
                {
                    if (bookingSnapshot.Exists)
                    {
                        bookingDataModel bookingData = bookingSnapshot.ConvertTo<bookingDataModel>();

                        DocumentReference roomReference = firestoreDb.Collection("room").Document(bookingData.roomID);
                        DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                        roomDataModel roomData = roomSnapshot.ConvertTo<roomDataModel>();

                        BookingModel bookingItem = new BookingModel()
                        {
                            BookingID = bookingSnapshot.Id,
                            Name = roomData.objName,
                            Num = 1,
                            RoomName = roomData.name,
                            timeList = bookingData.timeList
                        };
                        bookingList.Add(bookingItem);
                    }
                    else
                    {
                        Console.WriteLine("Document does not exist!", bookingSnapshot.Id);
                    }
                }
                return View(bookingList);
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }

        }

        public async Task<IActionResult> cancleAsync(string BookingID)
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                DocumentReference bookingReference = firestoreDb.Collection("booking").Document(BookingID);
                DocumentSnapshot bookingSnapshot = await bookingReference.GetSnapshotAsync();

                bookingDataModel bookingData = bookingSnapshot.ConvertTo<bookingDataModel>();

                if (bookingData.userID == uid)
                {
                    CollectionReference bookingCollection = firestoreDb.Collection("bookingCancle");
                    await bookingCollection.Document(bookingSnapshot.Id).SetAsync(bookingData);
                    await bookingReference.DeleteAsync();

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    Console.WriteLine("UserID not macth");
                    return RedirectToAction(nameof(Index));
                }


            }
            else
            {
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
                Console.WriteLine("ID token must not be null or empty");
                return null;
            }
        }

        [FirestoreData]
        private class bookingDataModel
        {
            [FirestoreProperty]
            public string roomID { get; set; }
            [FirestoreProperty]
            public string userID { get; set; }
            [FirestoreProperty]
            public List<DateTime> timeList { get; set; }

        }

        [FirestoreData]
        private class roomDataModel
        {
            [FirestoreProperty]
            public string name { get; set; }
            [FirestoreProperty]
            public string objName { get; set; }
        }

    }
}