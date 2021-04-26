using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using LockLock.Models;
using Newtonsoft.Json;

namespace LockLock.Controllers
{
    public class UserController : Controller
    {
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private string projectId;
        private FirestoreDb firestoreDb;

        public UserController()
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseJSON);
            projectId = "locklock-47b1d";
            firestoreDb = FirestoreDb.Create(projectId);
        }
        public async Task<IActionResult> Index()
        {
            Query userQuery = firestoreDb.Collection("user");
            QuerySnapshot userQuerySnapshot = await userQuery.GetSnapshotAsync();
            List<UserModel> listUser = new List<UserModel>();

            foreach (DocumentSnapshot documentSnapshot in userQuerySnapshot.Documents)
            {
                if (documentSnapshot.Exists)
                {
                    Dictionary<string, object> user = documentSnapshot.ToDictionary();
                    string json = JsonConvert.SerializeObject(user);
                    UserModel newUser = JsonConvert.DeserializeObject<UserModel>(json);
                    newUser.UserId = documentSnapshot.Id;
                    listUser.Add(newUser);
                }
            }
            return View(listUser);
        }

        [HttpGet]
        public IActionResult newUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> newUser(UserModel user)
        {
            CollectionReference collectionReference = firestoreDb.Collection("user");
            await collectionReference.AddAsync(user);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> updateUser(string userId)
        {
            DocumentReference documentReference = firestoreDb.Collection("user").Document(userId);
            DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();

            if (documentSnapshot.Exists)
            {
                UserModel user = documentSnapshot.ConvertTo<UserModel>();
                return View(user);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> updateUser(UserModel user)
        {
            DocumentReference documentReference = firestoreDb.Collection("user").Document(user.UserId);
            await documentReference.SetAsync(user, SetOptions.Overwrite);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> deleteUser(string userId)
        {
            DocumentReference documentReference = firestoreDb.Collection("user").Document(userId);
            await documentReference.DeleteAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}