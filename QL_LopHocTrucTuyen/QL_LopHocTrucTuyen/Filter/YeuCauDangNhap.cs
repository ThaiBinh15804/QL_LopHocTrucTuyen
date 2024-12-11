using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_LopHocTrucTuyen.Filter
{
    public class YeuCauDangNhap : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Kiểm tra nếu người dùng chưa đăng nhập
            if (HttpContext.Current.Session["User"] == null)
            {
                // Lấy tên controller và action hiện tại
                var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
                var actionName = filterContext.ActionDescriptor.ActionName;

                // Nếu trang học viên chưa đăng nhập, thì chuyển hướng đến trang đăng nhập
                if ((controllerName == "HocVien" && actionName == "HocTap"))
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary
                        {
                            { "controller", "HocVien" },
                            { "action", "DangNhap" }
                        });
                    // Thêm thông báo vào TempData
                    filterContext.Controller.TempData["warning"] = "Bạn cần phải đăng nhập";
                }
            }
            base.OnActionExecuting(filterContext);
        }
    }
}