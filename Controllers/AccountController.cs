
using System;
using System.Threading.Tasks;
using Firebase.Auth;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LockLock.Controllers
{
    public class AccountController : Controller
    {
        private FirebaseAuthProvider auth;
        private FirestoreDb firestoreDb;

        public AccountController()
        {
            auth = new FirebaseAuthProvider(
                            new FirebaseConfig("AIzaSyDYMUB0qohsGyFfdHCFWyxfcwr84HC-WCU"));

            string projectId = "locklock-47b1d";
            firestoreDb = FirestoreDb.Create(projectId);
        }

        public IActionResult Index()
        {
            var token = HttpContext.Session.GetString("_UserToken");
            if (token != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("SignIn");
            }
        }

        public IActionResult SignUp()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpModel singUpModel, UserModel userModel)
        {
            try
            {
                //create the user
                await auth.CreateUserWithEmailAndPasswordAsync(singUpModel.Email, singUpModel.Password, singUpModel.Firstname + " " + singUpModel.Lastname, false);
                //log in the new user
                var fbAuthLink = await auth
                                .SignInWithEmailAndPasswordAsync(singUpModel.Email, singUpModel.Password);
                string token = fbAuthLink.FirebaseToken;
                //saving the token in a session variable
                if (token != null)
                {
                    FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    CollectionReference userCollection = firestoreDb.Collection("user");
                    await userCollection.Document(decodedToken.Uid).SetAsync(singUpModel);

                    HttpContext.Session.SetString("_UserToken", token);
                    return RedirectToAction("Index","User");
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return View();
            }
        }

        public IActionResult SignIn()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SignIn(SignInModel model)
        {
            try
            {
                var fbAuthLink = await auth
                                           .SignInWithEmailAndPasswordAsync(model.Email, model.Password);
                string token = fbAuthLink.FirebaseToken;
                Console.Write("Token : ");
                Console.WriteLine(token);
                Console.WriteLine("");

                //saving the token in a session variable
                if (token != null)
                {
                    FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                    HttpContext.Session.SetString("_UserToken", token);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View();
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                ModelState.AddModelError(string.Empty, "Invalid username or password. Ex");
                return View();
            }

        }

    }
}