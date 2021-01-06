using Quiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace Quiz.Controllers
{
    public class HomeController : Controller
    {
        DBQUIZEntities db = new DBQUIZEntities();
        [HttpGet]

        public ActionResult StudentRegister()
        {

            return View();
        }
        [HttpPost]
        public ActionResult StudentRegister(Student st,HttpPostedFileBase imgfile)
        {
            Student s = new Student();
            try
            {
                s.StudentName = st.StudentName;
                s.StudentPassword = st.StudentPassword;
                s.StudentImage = uploadImage(imgfile);
                db.Students.Add(s);
                db.SaveChanges();
                return RedirectToAction("SLogin");

            }
            catch(Exception)
            {
                ViewBag.msg = "Data could not be inserted...";
            }
            
            return View();
        }
        public string uploadImage(HttpPostedFileBase imgfile)
        {
            string path = "-1";
            try
            {
                if(imgfile!=null && imgfile.ContentLength>0)
                {
                    string extension = Path.GetExtension(imgfile.FileName);
                    if(extension.ToLower().Equals("jpg") || extension.ToLower().Equals("jpeg") || extension.ToLower().Equals("png"))
                    {
                        Random r = new Random();
                        path = Path.Combine(Server.MapPath("~/Content/img"),r+Path.GetFileName(imgfile.FileName));
                        imgfile.SaveAs(path);
                        path = "~/Content/img" + r + Path.GetFileName(imgfile.FileName);


                    }
                }
                
            }
            catch(Exception)
            {
                throw;
            }
            return path;
        }


        [HttpGet]
        public ActionResult Logout()
        {
            Session.Abandon();
            Session.RemoveAll();

            return RedirectToAction("Index");

            
        }
        public ActionResult TLogin()
        {
            
            return View();
        }
        [HttpPost]
        public ActionResult TLogin(Admin a )
        {
            Admin ad = db.Admins.Where(x => x.AdminName == a.AdminName && x.AdminPassword == a.AdminPassword).SingleOrDefault();
            if(ad!= null)
            {
                Session["AdminID"] = ad.AdminID;
                return RedirectToAction("Dashboard");
            }
            else
            {
                ViewBag.msg = "Invalid username or password.";
            }
            return View();
        }
        public ActionResult SLogin()
        {
            return View();
        }
        [HttpPost]
        public ActionResult SLogin(Student s)
        {
            Student st = db.Students.Where(x => x.StudentName == s.StudentName && x.StudentPassword == s.StudentPassword).SingleOrDefault();
            if (st == null)
            {
                ViewBag.msg = "Invalid Email or Password";
            }
            else
            {
                Session["StudentID"] = st.StudentID;
                return RedirectToAction("StudentQuiz");
            }
            return View();
        }

        public ActionResult StudentQuiz()
        {
            if (Session["StudentID"] == null)
            {
                return RedirectToAction("SLogin");
            }
            return View();
        }
        
        [HttpPost]
        public ActionResult StudentQuiz(string room)
        {
           List <Category> list = db.Categories.ToList();
            
            foreach(var item in list)
            {
                if (item.CategoryEncryptedString == room)
                {

                    List<Question> li = db.Questions.Where(x => x.Category == item.CategoryID).ToList();

                    Queue<Question> queue = new Queue<Question>();
                    foreach(Question a in li)
                    {
                        queue.Enqueue(a);
                    }
                    TempData["QuizID"] = item.CategoryID;
                    TempData["questions"] = queue;
                    TempData["score"] = 0;

                    TempData.Keep();
                    return RedirectToAction("QuizStart");
                }
                else
                {
                    ViewBag.error = "No Quiz with this PIN found...";
                }
            }
            return View();
        }

        
        public ActionResult ScoreValue(string answer, int questionID)
        {
            string dbAnswer = db.Questions.SingleOrDefault(x => x.QuestionID == questionID).CorrectOption;
            if (answer.Equals(dbAnswer))
            {
                TempData["score"] = 1+Convert.ToInt32(TempData["score"]);
            }
            return RedirectToAction("QuizStart"); 
        }



        public ActionResult QuizStart()
        {
            if (Session["StudentID"] == null)
            {
                return RedirectToAction("SLogin");
            }

            Question q = null;
            if(TempData["questions"]!=null)
            {
                Queue<Question> qlist = (Queue<Question>) TempData["questions"];
                if(qlist.Count>0)
                {
                    q = qlist.Peek();
                    qlist.Dequeue();
                    TempData["questions"] = qlist;
                    TempData.Keep();
                    return View(q);

                }
                else
                {
                    return RedirectToAction("EndQuiz");
                }
            }
            else
            {
                return RedirectToAction("StudentQuiz");
            }
        }

        [HttpPost]
        public ActionResult QuizStart(Question q)
        {

            string correctAnswer=null;

            if (q.OptionA!=null)
            {
                correctAnswer = "A";
            }
            else if (q.OptionB!=null)
            {
                correctAnswer = "B";
            }
            else if (q.OptionC!=null)
            {
                correctAnswer = "B";
            }
            else if (q.OptionD!=null)
            {
                correctAnswer = "D";
            }
            if(correctAnswer.Equals(q.CorrectOption))
            TempData["score"] = Convert.ToInt32(TempData["score"])+1;
            TempData.Keep();
            return RedirectToAction("QuizStart");
        }

        public ActionResult viewAllQuestions(int? id)
        {
            if (Session["AdminID"] == null)
            {
                return RedirectToAction("TLogin");
            }
            if (id==null)
            {
                return RedirectToAction("Dashboard");
            }

            

            return View(db.Questions.Where(x => x.Category == id).ToList());
        }

        public ActionResult Dashboard()
        {
            if (Session["AdminID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpGet]
        public ActionResult AddCategory()
        {
            if(Session["AdminID"]==null)
            {
                return RedirectToAction("Index");
            }
           //Session["AdminID"] = 1;
            int adid = Convert.ToInt32(Session["AdminID"].ToString());
            List<Category> li = db.Categories.Where(x => x.CategoryAdmin == adid).OrderByDescending(x => x.CategoryID).ToList();
            ViewData["list"] = li;

            return View();

        }
        [HttpPost]
        public ActionResult AddCategory(Category cat)
        {
            
            List<Category> li = db.Categories.OrderByDescending(x => x.CategoryID).ToList();
            ViewData["list"] = li;

            Random r = new Random();
            Category c = new Category();
            c.CategoryName = cat.CategoryName;
            c.CategoryEncryptedString = Cryptor.Encrypt(cat.CategoryName.Trim() + r.Next().ToString(),true);
            c.CategoryAdmin = Convert.ToInt32(Session["AdminID"].ToString());
            db.Categories.Add(c);

            db.SaveChanges();

            return RedirectToAction("AddCategory");

        }
        [HttpGet]
        public ActionResult AddQuestion()
        {
            int sid = Convert.ToInt32(Session["AdminID"]);
            List<Category> li = db.Categories.Where(x => x.CategoryAdmin == sid).ToList();
            ViewBag.list = new SelectList(li, "CategoryID", "CategoryName");

            return View();
        }
        [HttpPost]
        public ActionResult AddQuestion(Question q)
        {
            int sid = Convert.ToInt32(Session["AdminID"]);
            List<Category> li = db.Categories.Where(x => x.CategoryAdmin == sid).ToList();
            ViewBag.list = new SelectList(li, "CategoryID", "CategoryName");

            Question qa = new Question();
            qa.QuestionText = q.QuestionText;
            qa.OptionA = q.OptionA;
            qa.OptionB = q.OptionB;
            qa.OptionC = q.OptionC;
            qa.OptionD = q.OptionD;
            qa.CorrectOption = q.CorrectOption;
            qa.Category = q.Category;

            db.Questions.Add(qa);
            db.SaveChanges();
            TempData["msg"] = "Question added successfully...";
            TempData.Keep();

            return RedirectToAction("AddQuestion");
            
        }

        public ActionResult EndQuiz()
        {
            int score = Convert.ToInt32(TempData["score"]);
            return View(score);
        }

        

        public ActionResult Index()
        {
            if(Session["AdminID"] != null)
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}