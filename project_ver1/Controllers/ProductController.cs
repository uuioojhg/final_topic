﻿using Microsoft.AspNetCore.Mvc;
using project_ver1.Models;
using System;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Data;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using static project_ver1.Controllers.ProductController;
using System.Collections.Generic;

// import HttpContext.Session.GetObject<T>('key') and HttpContext.Session.SetObject<T>('key', 'value')
using MyWebsite.Extensions;
using NuGet.Packaging;

namespace project_ver1.Controllers
{
    public class ProductController : Controller
    {
        private readonly HomeDbContext _context;
        private OrderData orderData;
        public ProductController(HomeDbContext context)
        {
            //_logger = logger; 
            _context = context;

        }
        public IActionResult Index()
        {
            var ap = _context.AgriculturalProduct.ToList();
            SetUserViewBag();
            return View(ap);
        }

        public IActionResult ShoppingCart()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {

                SetUserViewBag();
                return View();
            }
            else
            {

                return Redirect("/Home/member");
            }
        }


        [HttpPost]
        public IActionResult Index(int myCategoryID)
        {
            var product = _context.AgriculturalProduct.ToList();
            if (myCategoryID > 0)
            {
                product = null;
                product = _context.AgriculturalProduct.Where(p => p.CategoryID == myCategoryID).ToList();
            }
            SetUserViewBag();
            return View(product);
        }

        public ActionResult EachProductDetails(int? id)
        {
            var product = _context.AgriculturalProduct.Find(id);
            SetUserViewBag();
            return View(product);
        }

        [HttpPost]
        public IActionResult ShoppingCart(SetOrder setOrder)
        {
            SetUserViewBag();
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                if (HttpContext.Session.GetObject<OrderData>("cart") == null)
                {
                    orderData = new OrderData();
                    orderData.SumPrice = 0;
                    orderData.CustomerID = (int)HttpContext.Session.GetInt32("UserId");
                    orderData.OderTime = DateTime.Now;
                    orderData.OrderFinished = false;
                    orderData.EmployeeID = null;
                }
                else
                {
                    orderData = HttpContext.Session.GetObject<OrderData>("cart");
                }

                DetailData setDetails = new DetailData
                {
                    ProductID = setOrder.ProductID,
                    Count = setOrder.Count,
                    Price = setOrder.Price
                };

                if (orderData.Details == null)
                {
                    orderData.Details = new List<DetailData>();
                }

                var existingDetail = orderData.Details.FirstOrDefault(d => d.ProductID == setOrder.ProductID);
                if (existingDetail != null)
                {
                    if (setOrder.Count == 0)
                    {
                        orderData.SumPrice -= existingDetail.Count * existingDetail.Price;
                        orderData.Details.Remove(existingDetail);
                    }
                    else
                    {
                        orderData.SumPrice -= existingDetail.Count * existingDetail.Price;
                        existingDetail.Count = setOrder.Count;
                        existingDetail.Price = setOrder.Price;
                        orderData.SumPrice += existingDetail.Count * existingDetail.Price;
                    }
                }
                else
                {
                    if (setOrder.Count != 0)
                    {
                        orderData.Details.Add(setDetails);
                        orderData.SumPrice += setOrder.Count * setOrder.Price;
                    }
                }

                HttpContext.Session.SetObject("cart", orderData);

                var list = new List<object>();
                var countList = new List<int>();
                var priceList = new List<int>();

                foreach (var item in orderData.Details)
                {
                    var query = _context.AgriculturalProduct.Find(item.ProductID);
                    list.Add(query);
                    countList.Add(item.Count);
                    priceList.Add((int)item.Price);
                }

                ViewBag.Product_Count = countList;
                ViewBag.Product_Price = priceList;

                return View(list);
            }
            else
            {
                return Redirect("/Home/member");
            }
        }



        public class SetOrder
        {

            public int Count { get; set; }
            public int ProductID { get; set; }
            public int Price { get; set; }

        }

        public class OrderData : Agricultural_Order
        {

            public List<DetailData> Details { get; set; }
            public OrderData()
            {
                Details = new List<DetailData>();
            }
        }
        public class DetailData : Agricultural_Order_Details
        {

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

        [HttpPost]
        public IActionResult SaveToDatabase()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {

                var orderData = HttpContext.Session.GetObject<OrderData>("cart");

                if (orderData != null)
                {

                    var agriculturalOrder = new Agricultural_Order
                    {
                        CustomerID = orderData.CustomerID,
                        OderTime = orderData.OderTime,
                        OrderFinished = orderData.OrderFinished,
                        EmployeeID = orderData.EmployeeID,
                        SumPrice = orderData.SumPrice
                    };


                    _context.AgriculturalOrder.Add(agriculturalOrder);
                    _context.SaveChanges();


                    foreach (var detail in orderData.Details)
                    {
                        var agriculturalOrderDetail = new Agricultural_Order_Details
                        {
                            OrderID = agriculturalOrder.ID,
                            ProductID = detail.ProductID,
                            Count = detail.Count,
                            Price = detail.Price
                        };

                        _context.AgriculturalOrderDetail.Add(agriculturalOrderDetail);

                        var product = _context.AgriculturalProduct.Find(detail.ProductID);
                        if (product != null)
                        {
                            product.Stock -= detail.Count;
                        }
                    }


                    _context.SaveChanges();


                    HttpContext.Session.Remove("cart");
                }


                return RedirectToAction("Index");
            }
            else
            {
                return Redirect("/Home/member");
            }
        }
        //========================================================
        //訂單客戶資料
        //訂單客戶資料修改頁面
        public IActionResult CustomerInformation()
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
        //會員資料修改
        [HttpPost]
        public IActionResult CustomerInformation(Customer customer)
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
                   

                    _context.SaveChanges();
                    HttpContext.Session.SetString("UserName", user.Name);
                }
                return RedirectToAction("CustomerInformation");
            }
            return View(customer);
        }
    }
}

