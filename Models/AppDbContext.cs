using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace FacultyManagementSystem.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicCalendar> AcademicCalendars { get; set; }

    public virtual DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseReassignmentLog> CourseReassignmentLogs { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Designation> Designations { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<FacultyChangeLog> FacultyChangeLogs { get; set; }

    public virtual DbSet<LeaveBalance> LeaveBalances { get; set; }

    public virtual DbSet<LeaveRequest> LeaveRequests { get; set; }

    public virtual DbSet<LeaveType> LeaveTypes { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<TimeSlot> TimeSlots { get; set; }

    public virtual DbSet<Timetable> Timetables { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwCourseDetail> VwCourseDetails { get; set; }

    public virtual DbSet<VwFacultyDetail> VwFacultyDetails { get; set; }

    public virtual DbSet<VwFacultyWorkload> VwFacultyWorkloads { get; set; }

    public virtual DbSet<VwLeaveRequest> VwLeaveRequests { get; set; }

    public virtual DbSet<VwTodaysTimetable> VwTodaysTimetables { get; set; }

    public virtual DbSet<WorkloadAssignment> WorkloadAssignments { get; set; }

    public virtual DbSet<WorkloadStandard> WorkloadStandards { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AcademicCalendar>(entity =>
        {
            entity.HasKey(e => e.CalId).HasName("PRIMARY");

            entity.ToTable("academic_calendar", tb => tb.HasComment("Holidays, exam weeks, study breaks per semester"));

            entity.HasIndex(e => e.CreatedBy, "fk_cal_user");

            entity.HasIndex(e => new { e.SemId, e.EventDate, e.EventType }, "uq_cal_sem_date").IsUnique();

            entity.Property(e => e.CalId).HasColumnName("cal_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EventDate).HasColumnName("event_date");
            entity.Property(e => e.EventName)
                .HasMaxLength(150)
                .HasColumnName("event_name");
            entity.Property(e => e.EventType)
                .HasDefaultValueSql("'Holiday'")
                .HasColumnType("enum('Holiday','MidExam','FinalExam','StudyBreak','Orientation','Emergency','WorkingDay')")
                .HasColumnName("event_type");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.IsTeaching).HasColumnName("is_teaching");
            entity.Property(e => e.SemId).HasColumnName("sem_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AcademicCalendars)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_cal_user");

            entity.HasOne(d => d.Sem).WithMany(p => p.AcademicCalendars)
                .HasForeignKey(d => d.SemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cal_sem");
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.ArId).HasName("PRIMARY");

            entity.ToTable("attendance_records", tb => tb.HasComment("Per-session faculty attendance records"));

            entity.HasIndex(e => e.CalId, "fk_att_cal");

            entity.HasIndex(e => e.SlotId, "fk_att_slot");

            entity.HasIndex(e => new { e.WaId, e.AttDate, e.SlotId }, "uq_att_wa_date_slot").IsUnique();

            entity.Property(e => e.ArId).HasColumnName("ar_id");
            entity.Property(e => e.AttDate).HasColumnName("att_date");
            entity.Property(e => e.CalId).HasColumnName("cal_id");
            entity.Property(e => e.Remarks)
                .HasColumnType("text")
                .HasColumnName("remarks");
            entity.Property(e => e.SlotId).HasColumnName("slot_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Present'")
                .HasColumnType("enum('Present','Absent','Late','Leave','Holiday')")
                .HasColumnName("status");
            entity.Property(e => e.WaId).HasColumnName("wa_id");

            entity.HasOne(d => d.Cal).WithMany(p => p.AttendanceRecords)
                .HasForeignKey(d => d.CalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_att_cal");

            entity.HasOne(d => d.Slot).WithMany(p => p.AttendanceRecords)
                .HasForeignKey(d => d.SlotId)
                .HasConstraintName("fk_att_slot");

            entity.HasOne(d => d.Wa).WithMany(p => p.AttendanceRecords)
                .HasForeignKey(d => d.WaId)
                .HasConstraintName("fk_att_wa");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PRIMARY");

            entity.ToTable("courses", tb => tb.HasComment("Course catalogue"));

            entity.HasIndex(e => e.DeptId, "fk_crs_dept");

            entity.HasIndex(e => e.CourseCode, "uq_course_code").IsUnique();

            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CourseCode)
                .HasMaxLength(20)
                .HasColumnName("course_code");
            entity.Property(e => e.CourseType)
                .HasDefaultValueSql("'Theory'")
                .HasColumnType("enum('Theory','Lab','Theory+Lab')")
                .HasColumnName("course_type");
            entity.Property(e => e.CreditHours).HasColumnName("credit_hours");
            entity.Property(e => e.DeptId).HasColumnName("dept_id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");

            entity.HasOne(d => d.Dept).WithMany(p => p.Courses)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crs_dept");
        });

        modelBuilder.Entity<CourseReassignmentLog>(entity =>
        {
            entity.HasKey(e => e.RlId).HasName("PRIMARY");

            entity.ToTable("course_reassignment_log", tb => tb.HasComment("Audit: course reassignment history"));

            entity.HasIndex(e => e.CourseId, "idx_crl_course");

            entity.HasIndex(e => e.FromEmpId, "idx_crl_from_emp");

            entity.HasIndex(e => e.ReassignedBy, "idx_crl_reassigned");

            entity.HasIndex(e => e.SemId, "idx_crl_sem");

            entity.HasIndex(e => e.ToEmpId, "idx_crl_to_emp");

            entity.Property(e => e.RlId).HasColumnName("rl_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.FromEmpId).HasColumnName("from_emp_id");
            entity.Property(e => e.Reason)
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.ReassignedBy).HasColumnName("reassigned_by");
            entity.Property(e => e.ReassignedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("reassigned_on");
            entity.Property(e => e.SemId).HasColumnName("sem_id");
            entity.Property(e => e.ToEmpId).HasColumnName("to_emp_id");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseReassignmentLogs)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crl_course");

            entity.HasOne(d => d.FromEmp).WithMany(p => p.CourseReassignmentLogFromEmps)
                .HasForeignKey(d => d.FromEmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crl_from_emp");

            entity.HasOne(d => d.ReassignedByNavigation).WithMany(p => p.CourseReassignmentLogs)
                .HasForeignKey(d => d.ReassignedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_crl_user");

            entity.HasOne(d => d.Sem).WithMany(p => p.CourseReassignmentLogs)
                .HasForeignKey(d => d.SemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crl_sem");

            entity.HasOne(d => d.ToEmp).WithMany(p => p.CourseReassignmentLogToEmps)
                .HasForeignKey(d => d.ToEmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crl_to_emp");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DeptId).HasName("PRIMARY");

            entity.ToTable("departments", tb => tb.HasComment("University departments"));

            entity.HasIndex(e => e.DeptName, "uq_dept_name").IsUnique();

            entity.Property(e => e.DeptId).HasColumnName("dept_id");
            entity.Property(e => e.Contact)
                .HasMaxLength(50)
                .HasColumnName("contact");
            entity.Property(e => e.DeptName)
                .HasMaxLength(100)
                .HasColumnName("dept_name");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.HodName)
                .HasMaxLength(100)
                .HasColumnName("hod_name");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        });

        modelBuilder.Entity<Designation>(entity =>
        {
            entity.HasKey(e => e.DesignationId).HasName("PRIMARY");

            entity.ToTable("designations", tb => tb.HasComment("Lookup: faculty designation types"));

            entity.HasIndex(e => e.DesignationName, "uq_designation_name").IsUnique();

            entity.Property(e => e.DesignationId).HasColumnName("designation_id");
            entity.Property(e => e.DesignationName)
                .HasMaxLength(80)
                .HasColumnName("designation_name");
            entity.Property(e => e.RankOrder).HasColumnName("rank_order");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.EmpId).HasName("PRIMARY");

            entity.ToTable("faculty", tb => tb.HasComment("Faculty member profiles"));

            entity.HasIndex(e => e.DeptId, "fk_fac_dept");

            entity.HasIndex(e => e.DesignationId, "fk_fac_desig");

            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.DeptId).HasColumnName("dept_id");
            entity.Property(e => e.DesignationId).HasColumnName("designation_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.EmpType)
                .HasDefaultValueSql("'Permanent'")
                .HasColumnType("enum('Permanent','Visiting','Contractual')")
                .HasColumnName("emp_type");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Qualification)
                .HasColumnType("text")
                .HasColumnName("qualification");

            entity.HasOne(d => d.Dept).WithMany(p => p.Faculties)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fac_dept");

            entity.HasOne(d => d.Designation).WithMany(p => p.Faculties)
                .HasForeignKey(d => d.DesignationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fac_desig");
        });

        modelBuilder.Entity<FacultyChangeLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PRIMARY");

            entity.ToTable("faculty_change_log", tb => tb.HasComment("Audit: faculty profile change history"));

            entity.HasIndex(e => e.EmpId, "fk_fcl_emp");

            entity.HasIndex(e => e.ChangedBy, "fk_fcl_user");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.ChangeType)
                .HasColumnType("enum('Designation','Department','EmploymentType','Status')")
                .HasColumnName("change_type");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.ChangedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("changed_on");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.NewValue)
                .HasMaxLength(100)
                .HasColumnName("new_value");
            entity.Property(e => e.OldValue)
                .HasMaxLength(100)
                .HasColumnName("old_value");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.FacultyChangeLogs)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_fcl_user");

            entity.HasOne(d => d.Emp).WithMany(p => p.FacultyChangeLogs)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fcl_emp");
        });

        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(e => e.LbId).HasName("PRIMARY");

            entity.ToTable("leave_balances", tb => tb.HasComment("Leave entitlement balances per faculty per year"));

            entity.HasIndex(e => e.LtId, "fk_lb_lt");

            entity.HasIndex(e => new { e.EmpId, e.LtId, e.AcadYear }, "uq_lb_emp_lt_year").IsUnique();

            entity.Property(e => e.LbId).HasColumnName("lb_id");
            entity.Property(e => e.AcadYear)
                .HasMaxLength(10)
                .HasColumnName("acad_year");
            entity.Property(e => e.BalanceRemaining).HasColumnName("balance_remaining");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.LtId).HasColumnName("lt_id");
            entity.Property(e => e.TotalEntitled).HasColumnName("total_entitled");

            entity.HasOne(d => d.Emp).WithMany(p => p.LeaveBalances)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lb_emp");

            entity.HasOne(d => d.Lt).WithMany(p => p.LeaveBalances)
                .HasForeignKey(d => d.LtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lb_lt");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.LrId).HasName("PRIMARY");

            entity.ToTable("leave_requests", tb => tb.HasComment("Leave applications submitted by faculty"));

            entity.HasIndex(e => e.ApprovedBy, "fk_lr_approved_by");

            entity.HasIndex(e => e.EmpId, "fk_lr_emp");

            entity.HasIndex(e => e.LtId, "fk_lr_lt");

            entity.Property(e => e.LrId).HasColumnName("lr_id");
            entity.Property(e => e.ApprRemarks)
                .HasColumnType("text")
                .HasColumnName("appr_remarks");
            entity.Property(e => e.ApprStatus)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Approved','Rejected')")
                .HasColumnName("appr_status");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.LtId).HasColumnName("lt_id");
            entity.Property(e => e.Reason)
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.SubmittedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("submitted_on");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_lr_approved_by");

            entity.HasOne(d => d.Emp).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lr_emp");

            entity.HasOne(d => d.Lt).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.LtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lr_lt");
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(e => e.LtId).HasName("PRIMARY");

            entity.ToTable("leave_types", tb => tb.HasComment("Lookup: leave categories"));

            entity.HasIndex(e => e.LtName, "uq_lt_name").IsUnique();

            entity.Property(e => e.LtId).HasColumnName("lt_id");
            entity.Property(e => e.DefaultEntitlement).HasColumnName("default_entitlement");
            entity.Property(e => e.IsPaid)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_paid");
            entity.Property(e => e.LtName)
                .HasMaxLength(50)
                .HasColumnName("lt_name");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemId).HasName("PRIMARY");

            entity.ToTable("semesters", tb => tb.HasComment("Academic semester records"));

            entity.HasIndex(e => e.SemName, "uq_sem_name").IsUnique();

            entity.Property(e => e.SemId).HasColumnName("sem_id");
            entity.Property(e => e.AcadYear)
                .HasMaxLength(10)
                .HasColumnName("acad_year");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsCurrent).HasColumnName("is_current");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.SemName)
                .HasMaxLength(50)
                .HasColumnName("sem_name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PRIMARY");

            entity.ToTable("time_slots", tb => tb.HasComment("Lookup: class time slots"));

            entity.HasIndex(e => e.SlotLabel, "uq_slot_label").IsUnique();

            entity.Property(e => e.SlotId).HasColumnName("slot_id");
            entity.Property(e => e.EndTime)
                .HasColumnType("time")
                .HasColumnName("end_time");
            entity.Property(e => e.SlotLabel)
                .HasMaxLength(30)
                .HasColumnName("slot_label");
            entity.Property(e => e.StartTime)
                .HasColumnType("time")
                .HasColumnName("start_time");
        });

        modelBuilder.Entity<Timetable>(entity =>
        {
            entity.HasKey(e => e.TtId).HasName("PRIMARY");

            entity.ToTable("timetable", tb => tb.HasComment("Weekly class schedule"));

            entity.HasIndex(e => e.SlotId, "fk_tt_slot");

            entity.HasIndex(e => e.WaId, "fk_tt_wa");

            entity.Property(e => e.TtId).HasColumnName("tt_id");
            entity.Property(e => e.ConflictFlag).HasColumnName("conflict_flag");
            entity.Property(e => e.DayOfWeek)
                .HasColumnType("enum('Monday','Tuesday','Wednesday','Thursday','Friday','Saturday')")
                .HasColumnName("day_of_week");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Room)
                .HasMaxLength(30)
                .HasColumnName("room");
            entity.Property(e => e.SlotId).HasColumnName("slot_id");
            entity.Property(e => e.WaId).HasColumnName("wa_id");

            entity.HasOne(d => d.Slot).WithMany(p => p.Timetables)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tt_slot");

            entity.HasOne(d => d.Wa).WithMany(p => p.Timetables)
                .HasForeignKey(d => d.WaId)
                .HasConstraintName("fk_tt_wa");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users", tb => tb.HasComment("System login accounts"));

            entity.HasIndex(e => e.EmpId, "fk_usr_emp");

            entity.HasIndex(e => e.Username, "uq_username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.LastLogin)
                .HasColumnType("datetime")
                .HasColumnName("last_login");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasDefaultValueSql("'Faculty'")
                .HasColumnType("enum('Admin','HOD','Faculty')")
                .HasColumnName("role");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");

            entity.HasOne(d => d.Emp).WithMany(p => p.Users)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_usr_emp");
        });

        modelBuilder.Entity<VwCourseDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_course_details");

            entity.Property(e => e.CourseCode)
                .HasMaxLength(20)
                .HasColumnName("course_code");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CourseType)
                .HasDefaultValueSql("'Theory'")
                .HasColumnType("enum('Theory','Lab','Theory+Lab')")
                .HasColumnName("course_type");
            entity.Property(e => e.CreditHours).HasColumnName("credit_hours");
            entity.Property(e => e.DeptId).HasColumnName("dept_id");
            entity.Property(e => e.DeptName)
                .HasMaxLength(100)
                .HasColumnName("dept_name");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Status)
                .HasMaxLength(8)
                .HasDefaultValueSql("''")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");
        });

        modelBuilder.Entity<VwFacultyDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_faculty_details");

            entity.Property(e => e.DeptId).HasColumnName("dept_id");
            entity.Property(e => e.DeptName)
                .HasMaxLength(100)
                .HasColumnName("dept_name");
            entity.Property(e => e.DesignationId).HasColumnName("designation_id");
            entity.Property(e => e.DesignationName)
                .HasMaxLength(80)
                .HasColumnName("designation_name");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.EmpType)
                .HasDefaultValueSql("'Permanent'")
                .HasColumnType("enum('Permanent','Visiting','Contractual')")
                .HasColumnName("emp_type");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Qualification)
                .HasColumnType("text")
                .HasColumnName("qualification");
            entity.Property(e => e.Status)
                .HasMaxLength(8)
                .HasDefaultValueSql("''")
                .HasColumnName("status");
        });

        modelBuilder.Entity<VwFacultyWorkload>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_faculty_workload");

            entity.Property(e => e.AssignedDate).HasColumnName("assigned_date");
            entity.Property(e => e.CourseCode)
                .HasMaxLength(20)
                .HasColumnName("course_code");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(150)
                .HasColumnName("course_title");
            entity.Property(e => e.CreditHours).HasColumnName("credit_hours");
            entity.Property(e => e.DeptName)
                .HasMaxLength(100)
                .HasColumnName("dept_name");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.FacultyName)
                .HasMaxLength(100)
                .HasColumnName("faculty_name");
            entity.Property(e => e.SemId).HasColumnName("sem_id");
            entity.Property(e => e.SemName)
                .HasMaxLength(50)
                .HasColumnName("sem_name");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Active'")
                .HasColumnType("enum('Active','Dropped','Substituted')")
                .HasColumnName("status");
            entity.Property(e => e.TotalHours)
                .HasPrecision(5, 2)
                .HasColumnName("total_hours");
            entity.Property(e => e.WaId).HasColumnName("wa_id");
        });

        modelBuilder.Entity<VwLeaveRequest>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_leave_requests");

            entity.Property(e => e.AppliedOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("applied_on");
            entity.Property(e => e.ApprovalRemarks)
                .HasColumnType("text")
                .HasColumnName("approval_remarks");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.ApprovedByName)
                .HasMaxLength(50)
                .HasColumnName("approved_by_name");
            entity.Property(e => e.DeptName)
                .HasMaxLength(100)
                .HasColumnName("dept_name");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.FacultyName)
                .HasMaxLength(100)
                .HasColumnName("faculty_name");
            entity.Property(e => e.LeaveTypeName)
                .HasMaxLength(50)
                .HasColumnName("leave_type_name");
            entity.Property(e => e.LrId).HasColumnName("lr_id");
            entity.Property(e => e.Reason)
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pending'")
                .HasColumnType("enum('Pending','Approved','Rejected')")
                .HasColumnName("status");
            entity.Property(e => e.TotalDays).HasColumnName("total_days");
        });

        modelBuilder.Entity<VwTodaysTimetable>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_todays_timetable");

            entity.Property(e => e.ConflictFlag).HasColumnName("conflict_flag");
            entity.Property(e => e.CourseCode)
                .HasMaxLength(20)
                .HasColumnName("course_code");
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(150)
                .HasColumnName("course_title");
            entity.Property(e => e.DayOfWeek)
                .HasColumnType("enum('Monday','Tuesday','Wednesday','Thursday','Friday','Saturday')")
                .HasColumnName("day_of_week");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.EndTime)
                .HasColumnType("time")
                .HasColumnName("end_time");
            entity.Property(e => e.FacultyName)
                .HasMaxLength(100)
                .HasColumnName("faculty_name");
            entity.Property(e => e.Room)
                .HasMaxLength(30)
                .HasColumnName("room");
            entity.Property(e => e.SemName)
                .HasMaxLength(50)
                .HasColumnName("sem_name");
            entity.Property(e => e.SlotLabel)
                .HasMaxLength(30)
                .HasColumnName("slot_label");
            entity.Property(e => e.StartTime)
                .HasColumnType("time")
                .HasColumnName("start_time");
        });

        modelBuilder.Entity<WorkloadAssignment>(entity =>
        {
            entity.HasKey(e => e.WaId).HasName("PRIMARY");

            entity.ToTable("workload_assignments", tb => tb.HasComment("Faculty-course-semester assignments (central hub)"));

            entity.HasIndex(e => e.CourseId, "fk_wa_course");

            entity.HasIndex(e => e.SemId, "fk_wa_sem");

            entity.HasIndex(e => new { e.EmpId, e.CourseId, e.SemId }, "uq_wa_emp_course_sem").IsUnique();

            entity.Property(e => e.WaId).HasColumnName("wa_id");
            entity.Property(e => e.AssignedDate).HasColumnName("assigned_date");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.SemId).HasColumnName("sem_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Active'")
                .HasColumnType("enum('Active','Dropped','Substituted')")
                .HasColumnName("status");
            entity.Property(e => e.TotalHours)
                .HasPrecision(5, 2)
                .HasColumnName("total_hours");

            entity.HasOne(d => d.Course).WithMany(p => p.WorkloadAssignments)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wa_course");

            entity.HasOne(d => d.Emp).WithMany(p => p.WorkloadAssignments)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wa_emp");

            entity.HasOne(d => d.Sem).WithMany(p => p.WorkloadAssignments)
                .HasForeignKey(d => d.SemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wa_sem");
        });

        modelBuilder.Entity<WorkloadStandard>(entity =>
        {
            entity.HasKey(e => e.WsId).HasName("PRIMARY");

            entity.ToTable("workload_standards", tb => tb.HasComment("Credit hour thresholds per dept per semester"));

            entity.HasIndex(e => e.SemId, "fk_ws_sem");

            entity.HasIndex(e => new { e.DeptId, e.SemId }, "uq_ws_dept_sem").IsUnique();

            entity.Property(e => e.WsId).HasColumnName("ws_id");
            entity.Property(e => e.DeptId).HasColumnName("dept_id");
            entity.Property(e => e.MaxHours)
                .HasDefaultValueSql("'21'")
                .HasColumnName("max_hours");
            entity.Property(e => e.MinHours)
                .HasDefaultValueSql("'9'")
                .HasColumnName("min_hours");
            entity.Property(e => e.SemId).HasColumnName("sem_id");
            entity.Property(e => e.StdHours)
                .HasDefaultValueSql("'15'")
                .HasColumnName("std_hours");

            entity.HasOne(d => d.Dept).WithMany(p => p.WorkloadStandards)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ws_dept");

            entity.HasOne(d => d.Sem).WithMany(p => p.WorkloadStandards)
                .HasForeignKey(d => d.SemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ws_sem");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
