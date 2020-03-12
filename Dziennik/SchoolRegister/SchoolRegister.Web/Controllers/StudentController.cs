using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolRegister.BLL.Entities;
using SchoolRegister.Services.Interfaces;
using SchoolRegister.ViewModels.DTOs;
using SchoolRegister.ViewModels.VMs;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace SchoolRegister.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IGroupService _groupService;
        private readonly UserManager<User> _userManager;
        private readonly ITeacherService _teacherService;
        private readonly IGradeService _gradeService;

        public StudentController(IStudentService studentService, IGroupService groupService, UserManager<User> userManager, ITeacherService teacherService, IGradeService gradeService)
        {
            _studentService = studentService;
            _groupService = groupService;
            _userManager = userManager;
            _teacherService = teacherService;
            _gradeService = gradeService;
        }

        [Authorize(Roles = "Teacher, Admin, Parent")]
        public IActionResult Index()
        {

            IEnumerable<StudentVm> studentsVm = null;
            if (User.Identity.IsAuthenticated && User.IsInRole("Parent"))
            {
                var parent = _userManager.GetUserAsync(User).Result;
                studentsVm = _studentService.GetStudents(s => s.ParentId == parent.Id);
            }
            else if (User.Identity.IsAuthenticated && User.IsInRole("Teacher"))
            {
                var teacher = _userManager.Users.OfType<Teacher>().FirstOrDefault(x => x.UserName == User.Identity.Name);
                var student = teacher.Subjects.SelectMany(x => x.SubjectGroups.SelectMany(y => y.Group.Students));
                studentsVm = Mapper.Map<IEnumerable<StudentVm>>(student);
            }
            else if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                studentsVm = _studentService.GetStudents();
            }
            return View(studentsVm);
        }
        [Authorize(Roles = "Teacher, Admin, Parent, Student")]
        public IActionResult Details(int? studentId)
        {
            var getGradesDto = new GetGradesDto
            {
                StudentId = studentId ?? _userManager.GetUserAsync(User).Result.Id,
                GetterUserId = _userManager.GetUserAsync(User).Result.Id
            };
            var studentGradesReport = _gradeService.GetGradesReportForStudent(getGradesDto);
            if (studentGradesReport == null) return View("Error");
            return View(studentGradesReport);
        }


        [Authorize(Roles = "Teacher, Admin")]
        public IActionResult AttachStudentToGroup(int studentId)
        {
            ViewBag.ActionType = "Attach";
            return AttachDetachGetView(studentId);
        }


        [Authorize(Roles = "Teacher, Admin")]
        public IActionResult DetachStudentToGroup(int studentId)
        {
            ViewBag.ActionType = "Detach";
            return AttachDetachGetView(studentId);
        }


        [Authorize(Roles = "Teacher, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AttachStudentToGroup(AttachDetachStudentToGroupDto attachDetachStudentToGroupDto)
        {
            if (ModelState.IsValid)
            {
                _groupService.AttachStudentToGroup(attachDetachStudentToGroupDto);
                return RedirectToAction("Index");
            }

            return View();
        }


        [Authorize(Roles = "Teacher, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DetachStudentToGroup(AttachDetachStudentToGroupDto attachDetachStudentToGroupDto)
        {
            if (ModelState.IsValid)
            {
                _groupService.DetachStudentFromGroup(attachDetachStudentToGroupDto);
                return RedirectToAction("Index");
            }

            return View();
        }


        [Authorize(Roles = "Teacher, Admin")]
        private IActionResult AttachDetachGetView(int studentId)
        {
            var students = _studentService.GetStudents();
            var groups = _groupService.GetGroups();
            var currentStudent = students.FirstOrDefault(x => x.Id == studentId);
            if (currentStudent == null)
            {
                throw new ArgumentNullException("studentId not exists.");
            }

            var attachDetachStudentToGroupDto = new AttachDetachStudentToGroupDto
            {
                StudentId = currentStudent.Id
            };
            ViewBag.SubjectList = new SelectList(students.Select(s => new
            {
                Text = $"{s.FirstName} {s.LastName}",
                Value = s.Id,
                Selected = s.Id == currentStudent.Id
            }), "Value", "Text");
            ViewBag.GroupList = new SelectList(groups.Select(s => new
            {
                Text = s.Name,
                Value = s.Id
            }), "Value", "Text");
            return View("AttachDetachStudentToGroup", attachDetachStudentToGroupDto);
        }
    }


}