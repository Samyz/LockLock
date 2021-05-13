using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Google.Type;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace LockLock.Controllers
{
    public class BlacklistController : Controller
    {
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirestoreDb firestoreDb;
        public BlacklistController()
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
            string adminUID = await verifyAdminTokenAsync();
            if (adminUID != null)
            {
                List<BlackListModel> userBlackList = new List<BlackListModel>();

                Query blacklistQuery = firestoreDb.Collection("blacklist").OrderBy("timeCreate");
                QuerySnapshot blacklistQuerySnapshot = await blacklistQuery.GetSnapshotAsync();

                foreach (DocumentSnapshot blacklistSnapshot in blacklistQuerySnapshot)
                {
                    if (blacklistSnapshot.Exists)
                    {
                        BlackListDataModel blacklistData = blacklistSnapshot.ConvertTo<BlackListDataModel>();

                        DocumentReference userReference = firestoreDb.Collection("users").Document(blacklistData.userID);
                        DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                        UserModel userData = userSnapshot.ConvertTo<UserModel>();

                        BlackListModel userBlacklistData = new BlackListModel()
                        {
                            BlacklistID = blacklistSnapshot.Id,
                            userID = blacklistData.userID,
                            Name = userData.Firstname + " " + userData.Lastname,
                            Tel = userData.Tel,
                            Email = userData.Email
                        };
                        userBlackList.Add(userBlacklistData);
                    }
                    else
                    {
                        Console.WriteLine("Document does not exist!", blacklistSnapshot.Id);
                    }
                }
                return View(userBlackList);
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }

        }
        public async Task<IActionResult> addAsync(string userID)
        {
            string adminUID = await verifyAdminTokenAsync();
            if(userID == null) return BadRequest();
            if (adminUID != null)
            {
        
                BlackListDataModel  blackListData = new BlackListDataModel(){
                    userID = userID,
                    timeCreate = System.DateTime.UtcNow
                };
                await firestoreDb.Collection("blacklist").AddAsync(blackListData);
                return Ok();
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }
        public async Task<IActionResult> cancleAsync(string id)
        {
            string adminUID = await verifyAdminTokenAsync();
            if (adminUID != null)
            {
                DocumentReference blacklistReference = firestoreDb.Collection("blacklist").Document(id);
                await blacklistReference.DeleteAsync();
                return Ok();
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

        [FirestoreData]
        public class BlackListDataModel
        {
            public string BlacklistID { get; set; }
            [FirestoreProperty]
            public System.DateTime timeCreate { get; set; }
            [FirestoreProperty]
            public string userID { get; set; }
        }

    }
}