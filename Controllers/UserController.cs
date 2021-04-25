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
        private string firebaseJSON = "D:\\Samyz\\KMITL\\3-2\\SoftStu\\project\\locklockconfigure.json";
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
            List<User> listUser = new List<User>();

            foreach (DocumentSnapshot documentSnapshot in userQuerySnapshot.Documents)
            {
                if (documentSnapshot.Exists)
                {
                    Dictionary<string, object> user = documentSnapshot.ToDictionary();
                    string json = JsonConvert.SerializeObject(user);
                    User newUser = JsonConvert.DeserializeObject<User>(json);
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
        public async Task<IActionResult> newUser(User user)
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
                User user = documentSnapshot.ConvertTo<User>();
                return View(user);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> updateUser(User user)
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