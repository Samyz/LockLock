
using System;
using System.Threading.Tasks;
using Firebase.Auth;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LockLock.Controllers
{
    public class AccountController : Controller
    {

        FirebaseAuthProvider auth;
        public AccountController()
        {
            auth = new FirebaseAuthProvider(
                            new FirebaseConfig("AIzaSyDYMUB0qohsGyFfdHCFWyxfcwr84HC-WCU"));
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
        public async Task<IActionResult> SignUp(SignUpModel model)
        {
            try
            {
                //create the user
                await auth.CreateUserWithEmailAndPasswordAsync(model.Email, model.Password, model.Firstname + " " + model.Lastname, false);
                //log in the new user
                var fbAuthLink = await auth
                                .SignInWithEmailAndPasswordAsync(model.Email, model.Password);
                string token = fbAuthLink.FirebaseToken;

                // Console.Write(token);
                // CollectionReference collectionReference = firestoreDb.Collection("user");
                // await collectionReference.AddAsync(user,);

                //saving the token in a session variable
                if (token != null)
                {
                    HttpContext.Session.SetString("_UserToken", token);
                    return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("SignIn");
                }
            }
            catch (Exception ex)
            {
                // Console.Write(ex);
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

                //saving the token in a session variable
                if (token != null)
                {
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
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View();
            }

        }

    }
}