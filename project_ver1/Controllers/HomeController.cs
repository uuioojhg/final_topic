using Microsoft.AspNetCore.Mvc;
using project_ver1.Models;
using System;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Net.Mail;


namespace project_ver1.Controllers
{
    public class HomeController : Controller
    {
        // private readonly ILogger<HomeController> _logger;
        private readonly HomeDbContext _context;
        private readonly EmailService _emailService;
        private static Dictionary<string, string> resetTokens = new Dictionary<string, string>(); // �Ω�Ȧs���m�K�X�� token

        //�s�WAppDbContext 
        public HomeController(HomeDbContext context, EmailService emailService)
        {
            //_logger = logger; 
            _context = context;
            _emailService = emailService;

            //�s�W

        }



        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Index()
        {
            SetUserViewBag();
            return View();
        }

        public ActionResult Park_information()
        {
            SetUserViewBag();
            return View();
        }


        public ActionResult member()
        {
            return View();
        }
        //[HttpPost]
        public IActionResult About()
        {
            SetUserViewBag();
            // _context.Contacts.Add(item);
            //_context.SaveChanges();
            return View();
        }
        public IActionResult Ecpay()
        {
            return View();
        }

        public ActionResult Order()
        {
            SetUserViewBag();
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                var orders = _context.AgriculturalOrder
                                      .Where(order => order.CustomerID == userId)
                                      .ToList();

                var AgriculturalOrderModels = new List<AgriculturalOrderModel>();

                foreach (var order in orders)
                {
                    var orderDetails = _context.AgriculturalOrderDetail
                                              .Where(detail => detail.OrderID == order.ID)
                                              .ToList();
                    var productIds = orderDetails.Select(detail => detail.ProductID).ToList();

                    var products = _context.AgriculturalProduct
                                           .Where(product => productIds.Contains(product.ID))
                                           .ToList();

                    var productCategories = _context.AgriculturalProductsCategory
                                                    .Where(category => products.Select(product => product.CategoryID).Contains(category.ID))
                                                    .ToList();

                    AgriculturalOrderModels.Add(new AgriculturalOrderModel
                    {
                        Order = order,
                        OrderDetails = orderDetails,
                        Products = products,
                        ProductCategories = productCategories
                    });
                }

                var RoomOrders = _context.RoomOrder
                                      .Where(order => order.CustomerID == userId)
                                      .ToList();

                var RoomOrderModels = new List<RoomOrderModel>();

                foreach (var roomorder in RoomOrders)
                {


                    var orderDetails = _context.RoomOrderDetails
                                              .Where(detail => detail.OrderID == roomorder.ID)
                                              .ToList();

                    var roomIds = orderDetails.Select(detail => detail.RoomID).ToList();

                    var rooms = _context.Rooms
                                        .Where(room => roomIds.Contains(room.ID))
                                        .ToList();

                    var roomCategories = _context.RoomCategory
                                              .Where(category => rooms.Select(room => room.CategoryID).Contains(category.CategoryID))
                                              .ToList();

                    RoomOrderModels.Add(new RoomOrderModel
                    {
                        RoomOrder = roomorder,
                        RoomOrderDetails = orderDetails,
                        Rooms = rooms,
                        RoomCategory = roomCategories
                    });
                }

                var combinedViewModel = new CombinedOrderViewModel
                {
                    AgriculturalOrders = AgriculturalOrderModels,
                    RoomOrders = RoomOrderModels
                };

                return View(combinedViewModel);
            }
            else
            {
                return RedirectToAction("member", "Home");
            }
        }

        public IActionResult News(int CategoryID)
        {

            SetUserViewBag();
            List<News> newsList;

            if (CategoryID == 2)
            {
                newsList = _context.News.Where(news => news.CategoryID == 2).ToList();
            }
            else
            {
                newsList = _context.News.Where(news => news.CategoryID == 1).ToList();
            }


            return View(newsList);

        }







        [HttpPost]
        public IActionResult About(Contact appdb)
        {

            // �b�o�̳B�z�q��洣�檺�ƾ�
            // �z�i�H�N���x�s���Ʈw���A�o�e�l��A�Ϊ̶i���L�A���B�z

            // �o�̥u�O�@�ӥܨҡA�z�i�H�ھڦۤv���ݭn�i����
            DateTime now = DateTime.Now;
            Contact contact = new Contact
            {
                ID = appdb.ID,
                QuestionCategory = appdb.QuestionCategory,
                Name = appdb.Name,
                Sex = appdb.Sex,
                Phone = appdb.Phone,
                QuestionContent = appdb.QuestionContent,
                SetTime = now
            };
            _context.Contact.Add(contact);
            _context.SaveChanges();
            // ���]�z�Q�n�N���檺�ƾڶǻ���t�@�ӵ��Ϥ��H���
            return Redirect("/Home/Index");



        }

        public IActionResult Account()
        {

            // �w�n�J>����|�������A���n�J>����n�J����
            if (HttpContext.Session.GetInt32("UserId") != null)
            {

                return RedirectToAction("Members_Information", "Home");
            }
            else if (HttpContext.Session.GetInt32("EmployeeId") != null)
            {
                return RedirectToAction("Backstage", "Home");
            }
            else
            {

                return RedirectToAction("member", "Home");
            }


        }
        public IActionResult Backstage()
        {
            SetUserViewBag();
            if (HttpContext.Session.GetInt32("EmployeeId") != null)
            {
                var EmployeeId = HttpContext.Session.GetInt32("EmployeeId");
                var Employee = _context.Employee.FirstOrDefault(u => u.ID == EmployeeId);
                return View(Employee);
            }
            else
            {
                return RedirectToAction("member", "Home");
            }

        }

        public IActionResult BackstageContact()
        {
            SetUserViewBag();
            if (HttpContext.Session.GetInt32("EmployeeId") != null)
            {
                var backcontact = _context.Contact.ToList();


                return View(backcontact);
            }
            else
            {
                return RedirectToAction("member", "Home");
            }
        }

        //���u���������A�U�Ȱ��D���
        [HttpPost]
        public IActionResult ContactEdit(int contactId, int employeeId)
        {

            var contactToUpdate = _context.Contact.FirstOrDefault(c => c.ID == contactId);

            if (contactToUpdate != null)
            {

                contactToUpdate.Finished = true;
                contactToUpdate.EmployeeId = employeeId;
                _context.SaveChanges();
            }


            return RedirectToAction("BackstageContact");
        }

        //==============================================================
        //���u���~�q��
        public IActionResult BackstageProduct()
        {
            SetUserViewBag();
            if (HttpContext.Session.GetInt32("EmployeeId") != null)
            {
                var orders = _context.AgriculturalOrder
            .Include(order => order.Agricultural_Order_Details)
                .ThenInclude(detail => detail.Product)
            .Include(order => order.Customer)
            .ToList();
                return View(orders);

            }
            else
            {
                return RedirectToAction("member", "Home");
            }
        }


        [HttpPost]
        public IActionResult BackstageProductEdit(int orderId, int employeeId)
        {
            var orderUpdate = _context.AgriculturalOrder.FirstOrDefault(c => c.ID == orderId);

            if (orderUpdate != null)
            {
                orderUpdate.OrderFinished = true;
                orderUpdate.EmployeeID = employeeId;
                _context.SaveChanges();
            }

            return RedirectToAction("BackstageProduct");
        }

        //==============================================================


        //==============================================================
        //���u�q��
        public async Task<IActionResult> BackstageRoom()
        {
            SetUserViewBag();

            if (HttpContext.Session.GetInt32("EmployeeId") != null)
            {
                var roomOrders = await _context.RoomOrder
                    .Include(ro => ro.Customer)
                    .Include(ro => ro.Employee)
                    .Include(ro => ro.RoomOrderDetails)
                        .ThenInclude(rod => rod.Room)
                        .ThenInclude(r => r.Category)
                    .ToListAsync();

                var viewModel = roomOrders.Select(ro => new RoomOrderViewModel
                {
                    OrderID = ro.ID,
                    CustomerID = ro.CustomerID,
                    CustomerName = ro.Customer.Name,
                    CustomerPhone = ro.Customer.Phone,
                    CustomerAddress = ro.Customer.Address,
                    CustomerEmail = ro.Customer.Email,
                    OrderTime = ro.OrderTime,
                    CheckIn = ro.CheckIn,
                    CheckOut = ro.CheckOut,
                    OrderFinished = ro.OrderFinished,
                    EmployeeID = ro.EmployeeID,
                    EmployeeName = ro.Employee?.Name,
                    SumPrice = ro.SumPrice,
                    RoomOrderDetails = ro.RoomOrderDetails.Select(rod => new RoomOrderDetailViewModel
                    {
                        RoomID = rod.RoomID,
                        RoomCategory = rod.Room.Category.Name,
                        Price = rod.Price
                    }).ToList()
                }).ToList();

                return View(viewModel);
            }
            else
            {
                return RedirectToAction("Member", "Home");
            }
        }


        [HttpPost]
        public IActionResult BackstageRoomEdit(int orderId, int employeeId)
        {
            var orderUpdate = _context.RoomOrder.FirstOrDefault(c => c.ID == orderId);

            if (orderUpdate != null)
            {
                orderUpdate.OrderFinished = true;
                orderUpdate.EmployeeID = employeeId;
                _context.SaveChanges();
            }

            return RedirectToAction("BackstageRoom");
        }
        //==============================================================


        //==============================================================
        //���u�|����ƭק�
        public IActionResult BackstageMember()
        {
            SetUserViewBag();
            if (HttpContext.Session.GetInt32("EmployeeId") != null)
            {
                var backmember = _context.Customer.ToList();


                return View(backmember);
            }
            else
            {
                return RedirectToAction("member", "Home");
            }
        }

        public IActionResult MemberEdit(int id)
        {
            SetUserViewBag();
            var customer = _context.Customer.FirstOrDefault(c => c.ID == id);
            if (customer == null)
            {
                return NotFound();
            }
            return View("MemberEdit", customer);
        }

        [HttpPost]
        public IActionResult MemberEditPost(Customer customer)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Customer.FirstOrDefault(u => u.ID == customer.ID);
                if (user != null)
                {
                    user.Name = customer.Name;
                    user.Email = customer.Email;
                    user.Phone = customer.Phone;
                    user.Address = customer.Address;
                    user.Pwd = customer.Pwd;

                    _context.SaveChanges();
                    HttpContext.Session.SetString("UserName", user.Name);

                    return RedirectToAction("BackstageMember");
                }
            }
            return View("MemberEdit", customer);
        }

        [HttpPost]
        public IActionResult DeleteMember(int id)
        {
            var customer = _context.Customer.FirstOrDefault(c => c.ID == id);
            if (customer == null)
            {
                return NotFound();
            }

            _context.Customer.Remove(customer);
            _context.SaveChanges();

            return RedirectToAction("BackstageMember");
        }


        //==============================================================
        public IActionResult Members_join()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Members_join(Customer myuser)
        {
            ViewBag.DuplicateMessage = "�ӹq�l�H�c�w�Q���U";

            // �ˬd�q�l�H�c�O�_�w�g�s�b���Ʈw��
            var existingCustomer = _context.Customer.FirstOrDefault(c => c.Email == myuser.Email);
            if (existingCustomer != null)
            {
                //ModelState.AddModelError("Email", "�ӹq�l�H�c�w�g�Q���U�C");
                return View(myuser);
            }

            // �x�s�Τ���U��T���Ʈw
            _context.Customer.Add(myuser);
            _context.SaveChanges();
            HttpContext.Session.SetString("UserName", myuser.Name);
            HttpContext.Session.SetInt32("UserId", myuser.ID);

            TempData["SuccessMessage"] = "���U���\";
            // ���U���\�᭫�w�V�쭺���Ψ�L����
            //return View("Members_Information", "Home");

            // ��^���U��������ܿ��~�H��
            return View(myuser);
        }



        public IActionResult Members_Information()
        {
            SetUserViewBag();
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var user = _context.Customer.FirstOrDefault(u => u.ID == userId);
                return View(user);
            }
            else
            {
                return RedirectToAction("member", "Home");
            }

        }

        //�|����ƭקﭶ��
        public IActionResult CustomerEdit()
        {
            SetUserViewBag();
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var user = _context.Customer.FirstOrDefault(u => u.ID == userId);
                return View(user);
            }
            else
            {
                return RedirectToAction("member", "Home");
            }
        }
        //�|����ƭק�
        [HttpPost]
        public IActionResult CustomerEdit(Customer customer)
        {
            SetUserViewBag();
            if (ModelState.IsValid)
            {
                var user = _context.Customer.FirstOrDefault(u => u.ID == customer.ID);
                if (user != null)
                {
                    user.Name = customer.Name;
                    user.Email = customer.Email;
                    user.Phone = customer.Phone;
                    user.Address = customer.Address;
                    user.Pwd = customer.Pwd;

                    _context.SaveChanges();
                    HttpContext.Session.SetString("UserName", user.Name);
                }
                return RedirectToAction("Members_Information");
            }
            return View(customer);
        }

       
        [HttpPost]
        public IActionResult member(LoginViewModel model)
        {
            var user = _context.Customer.FirstOrDefault(u => u.Email == model.Email && u.Pwd == model.Password);
            var employee = _context.Employee.FirstOrDefault(u => u.Email == model.Email && u.Pwd == model.Password);

            if (employee != null)
            {
                HttpContext.Session.SetInt32("EmployeeId", employee.ID);
                HttpContext.Session.SetString("EmployeeName", employee.Name);
                return RedirectToAction("Backstage", "Home");
            }
            else
            {
                if (user != null)
                {
                    // �n�J���\�A��T�O�s��Session��
                    //HttpContext.Session.SetString("key", "value");
                    HttpContext.Session.SetInt32("UserId", user.ID);
                    HttpContext.Session.SetString("UserName", user.Name);

                    return RedirectToAction("Members_Information", "Home");
                }
                else
                {
                    ViewBag.ErrorMessage = "�b���αK�X�����T";
                    return View();
                }
            }
        }

        //��K�X
        public class EmailService
        {
            private readonly IConfiguration _configuration;

            public EmailService(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public async Task SendEmailAsync(string toEmail, string subject, string message)
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("�G���ܶ魫�]�K�X", _configuration["Smtp:Username"]));
                emailMessage.To.Add(new MailboxAddress("", toEmail));
                emailMessage.Subject = subject;
                emailMessage.Body = new TextPart("html")
                {
                    Text = message
                };

                // MailKit
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.Connect(_configuration["Smtp:Host"], int.Parse(_configuration["Smtp:Port"]), false);
                    client.Authenticate(_configuration["Smtp:Username"], _configuration["Smtp:Password"]);

                    await client.SendAsync(emailMessage);
                    client.Disconnect(true);
                }
            }
        }

        //�ø��
        private static string GenerateResetToken()
        {
            return Guid.NewGuid().ToString();
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Customer.FirstOrDefault(c => c.Email == model.Email);
                if (user != null)
                {
                    var resetToken = GenerateResetToken(); // �ͦ��H���X
                    resetTokens[resetToken] = user.Email; // �N token �s��r�夤
                    var resetLink = Url.Action("ResetPassword", "Home", new { token = resetToken }, Request.Scheme);

                    var message = $"���I���H�U�s���ӭ��]�z���K�X: <a href='{resetLink}'>���]�K�X</a>";
                    await _emailService.SendEmailAsync(user.Email, "���]�K�X", message);


                    return View("ForgotPasswordConfirmation");
                }
                else
                {
                    ViewBag.ErrorMessage = "���H�c�|�����U";
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (ModelState.IsValid)
            {

                // ��������Email
                if (resetTokens.TryGetValue(token, out string email))
                {
                    var model = new ResetPasswordViewModel { Token = token, Email = email };
                    return View(model);
                }
                else
                {
                    //  token �L��
                    return RedirectToAction("Index");
                }

            }
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {

            var user = _context.Customer.FirstOrDefault(c => c.Email == model.Email);
            if (user != null)
            {
                // �ק�K�X
                user.Pwd = model.Password;


                _context.SaveChanges();
            }
            TempData["SuccessMessage"] = "���\�ק�K�X";
            return View();

        }





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        public IActionResult Logout()
        {
            // �n�X�A�R��Session�����Τ��T
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("UserName");
            HttpContext.Session.Remove("EmployeeId");
            HttpContext.Session.Remove("EmployeeName");
            return RedirectToAction("Index", "Home");
        }
        private void SetUserViewBag()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var userName = HttpContext.Session.GetString("UserName");
                var userId = HttpContext.Session.GetInt32("UserId");

                ViewBag.UserName = userName;
                ViewBag.UserId = userId;
            }

            if (HttpContext.Session.GetInt32("EmployeeId") != null)
            {
                var EmployeeName = HttpContext.Session.GetString("EmployeeName");
                var EmployeeId = HttpContext.Session.GetInt32("EmployeeId");

                ViewBag.UserName = EmployeeName;
                ViewBag.UserId = EmployeeId;
            }
        }


    }


}
