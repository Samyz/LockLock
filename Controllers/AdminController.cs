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
    public class AdminController : Controller
    {
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirestoreDb firestoreDb;
        public AdminController()
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
            string adminUid = await verifyAdminTokenAsync();
            if (adminUid != null)
            {
                DocumentReference userReference = firestoreDb.Collection("users").Document(adminUid);
                DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                AdminModel admin = userSnapshot.ConvertTo<AdminModel>();

                Query roomQuery = firestoreDb.Collection("room").WhereEqualTo("adminID", adminUid);
                QuerySnapshot roomQuerySnapshot = await roomQuery.GetSnapshotAsync();

                List<RoomModel> roomList = new List<RoomModel>();

                foreach (string roomID in admin.rooms)
                {
                    DocumentReference roomReference = firestoreDb.Collection("room").Document(roomID);
                    DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                    RoomModel room = roomSnapshot.ConvertTo<RoomModel>();

                    room.RoomID = roomSnapshot.Id;
                    roomList.Add(room);
                }

                return View(roomList);
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }

        public async Task<IActionResult> updateRoomAsync(RoomModel room)
        {
            string adminUid = await verifyAdminTokenAsync();
            if (adminUid != null)
            {
                DocumentReference roomReference = firestoreDb.Collection("room").Document(room.RoomID);
                await roomReference.SetAsync(room, SetOptions.Overwrite);

                return RedirectToAction(nameof(Index));
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