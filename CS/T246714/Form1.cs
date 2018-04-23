using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils.Drawing;
using DevExpress.XtraScheduler;
using DevExpress.XtraScheduler.Drawing;

namespace T246714 {
    public partial class Form1 : Form {
        private bool drag = false;

        public Form1() {
            InitializeComponent();
            schedulerControl1.GroupType = SchedulerGroupType.Resource;
            schedulerControl1.OptionsCustomization.AllowAppointmentMultiSelect = false;
            schedulerControl1.Start = DateTime.Today;
            InitializeStorage();
            schedulerControl1.CustomDrawAppointment += schedulerControl1_CustomDrawAppointment;
            schedulerControl1.AppointmentDrag += schedulerControl1_AppointmentDrag;
            schedulerControl1.AppointmentDrop += schedulerControl1_AppointmentDrop;
        }

        void schedulerControl1_AppointmentDrop(object sender, AppointmentDragEventArgs e) {
            drag = false;
        }

        private void InitializeStorage() {
            AppointmentCustomFieldMapping safeIntervalStartCustomField = new AppointmentCustomFieldMapping("SafeIntervalStartCF", "SafeIntervalStartDB", FieldValueType.DateTime);
            schedulerStorage1.Appointments.CustomFieldMappings.Add(safeIntervalStartCustomField);
            AppointmentCustomFieldMapping safeIntervalEndCustomField = new AppointmentCustomFieldMapping("SafeIntervalEndCF", "SafeIntervalEndDB", FieldValueType.DateTime);
            schedulerStorage1.Appointments.CustomFieldMappings.Add(safeIntervalEndCustomField);

            Appointment apt1 = schedulerStorage1.CreateAppointment(AppointmentType.Normal);
            apt1.Subject = "TestSubject1";
            apt1.Start = DateTime.Today.AddHours(1);
            apt1.Duration = TimeSpan.FromHours(1);
            apt1.CustomFields["SafeIntervalStartCF"] = apt1.Start.AddHours(-1);
            apt1.CustomFields["SafeIntervalEndCF"] = apt1.End.AddHours(1);
            schedulerStorage1.Appointments.Add(apt1);

            Appointment apt2 = schedulerStorage1.CreateAppointment(AppointmentType.Normal);
            apt2.Subject = "TestSubject2";
            apt2.Start = DateTime.Today.AddHours(5);
            apt2.Duration = TimeSpan.FromHours(1);
            apt2.CustomFields["SafeIntervalStartCF"] = apt2.Start.AddHours(-1);
            apt2.CustomFields["SafeIntervalEndCF"] = apt2.End.AddHours(1);
            schedulerStorage1.Appointments.Add(apt2);

            schedulerStorage1.AppointmentInserting +=schedulerStorage1_AppointmentInserting;
            schedulerStorage1.AppointmentChanging += schedulerStorage1_AppointmentChanging;
        }

        void schedulerStorage1_AppointmentChanging(object sender, PersistentObjectCancelEventArgs e) {
            Appointment apt = (Appointment)e.Object;
            e.Cancel = (apt.Start < (DateTime)apt.CustomFields["SafeIntervalStartCF"] || apt.End > (DateTime)apt.CustomFields["SafeIntervalEndCF"]);
        }

        void schedulerStorage1_AppointmentInserting(object sender, PersistentObjectCancelEventArgs e) {
 	        Appointment apt = (Appointment)e.Object;
            apt.CustomFields["SafeIntervalStartCF"] = apt.Start.AddHours(-1);
            apt.CustomFields["SafeIntervalEndCF"] = apt.End.AddHours(1);
        }

        void schedulerControl1_AppointmentDrag(object sender, AppointmentDragEventArgs e) {
            SchedulerControl scheduler = (SchedulerControl)sender;
            Appointment apt = e.SourceAppointment;
            DateTime safeIntervalStart = (DateTime)apt.CustomFields["SafeIntervalStartCF"];
            DateTime safeIntervalEnd = (DateTime)apt.CustomFields["SafeIntervalEndCF"];
            e.Allow = e.EditedAppointment.Start >= safeIntervalStart && e.EditedAppointment.End <= safeIntervalEnd;
            drag = true;
        }

        void schedulerControl1_CustomDrawAppointment(object sender, CustomDrawObjectEventArgs e) {
            SchedulerControl scheduler = (SchedulerControl)sender;
            if(scheduler.ActiveViewType == SchedulerViewType.Day) {
                AppointmentViewInfo aptViewInfo = (AppointmentViewInfo)e.ObjectInfo;
                if(aptViewInfo.Selected) {
                    Appointment apt = aptViewInfo.Appointment;
                    SolidBrush brush = new SolidBrush((drag ? Color.FromArgb(32, 0, 0, 0) : Color.FromArgb(16, 0, 0, 0)));
                    TimeSpan timeScale = scheduler.DayView.TimeScale;
                    DateTime safeIntervalStart = (DateTime)apt.CustomFields["SafeIntervalStartCF"];
                    DateTime safeIntervalEnd = (DateTime)apt.CustomFields["SafeIntervalEndCF"];
                    int cellsCount = (int)((safeIntervalEnd - safeIntervalStart).Ticks / timeScale.Ticks);
                    SchedulerViewCellBaseCollection cells = scheduler.DayView.ViewInfo.CellContainers[0].Cells;
                    Rectangle topBounds = Rectangle.Empty;
                    Rectangle bottomBounds = Rectangle.Empty;
                    List<Rectangle> secondaryBounds = new List<Rectangle>();
                    Rectangle generalBounds = Rectangle.Empty;
                    InitializeBoundsInfo(safeIntervalStart, safeIntervalEnd, cells, ref topBounds, ref bottomBounds, secondaryBounds, ref generalBounds, aptViewInfo.Bounds);
                    Draw(e, brush, ref topBounds, ref bottomBounds, secondaryBounds, ref generalBounds);
                }
            }
        }

        private void Draw(CustomDrawObjectEventArgs e, SolidBrush brush, ref Rectangle topBounds, ref Rectangle bottomBounds, List<Rectangle> secondaryBounds, ref Rectangle generalBounds) {
            Region clip = e.Graphics.Clip;
            e.DrawDefault();
            Region newClip = new Region(generalBounds);
            e.Graphics.Clip = newClip;
            e.Graphics.FillRectangle(brush, topBounds);
            e.Graphics.DrawLines(new Pen(Color.Black, 1), new Point[] {
                new Point(topBounds.X, topBounds.Y + topBounds.Height - 1),
                new Point(topBounds.X, topBounds.Y),
                new Point(topBounds.X + topBounds.Width - 1, topBounds.Y),
                new Point(topBounds.X + topBounds.Width - 1, topBounds.Y + topBounds.Height - 1)
            });
            foreach(Rectangle r in secondaryBounds) {
                e.Graphics.FillRectangle(brush, r);
                e.Graphics.DrawLines(new Pen(Color.Black, 1), new Point[] {
                    new Point(r.X, r.Y),
                    new Point(r.X, r.Y + r.Height - 1)
                });
                e.Graphics.DrawLines(new Pen(Color.Black, 1), new Point[] {
                    new Point(r.X + r.Width - 1, r.Y),
                    new Point(r.X + r.Width - 1, r.Y + r.Height - 1)
                });
            }
            e.Graphics.FillRectangle(brush, bottomBounds);
            e.Graphics.DrawLines(new Pen(Color.Black, 1), new Point[] {
                new Point(bottomBounds.X, bottomBounds.Y),
                new Point(bottomBounds.X, bottomBounds.Y + bottomBounds.Height - 1),
                new Point(bottomBounds.X + bottomBounds.Width - 1, bottomBounds.Y + bottomBounds.Height - 1),
                new Point(bottomBounds.X + bottomBounds.Width - 1, bottomBounds.Y)
            });
            e.Graphics.Clip = clip;
            e.Handled = true;
        }

        private static void InitializeBoundsInfo(DateTime safeIntervalStart, DateTime safeIntervalEnd, SchedulerViewCellBaseCollection cells, ref Rectangle topBounds, ref Rectangle bottomBounds, List<Rectangle> secondaryBounds, ref Rectangle generalBounds, Rectangle aptBounds) {
            for(int i = 0; i < cells.Count; i++) {
                SchedulerViewCellBase cell = cells[i];
                if(cell.Interval.Contains(safeIntervalStart) && cell.Interval.End > safeIntervalStart) {
                    topBounds = cell.Bounds;
                    topBounds.Width = aptBounds.Width;
                    topBounds.X = aptBounds.X;
                } else if(cell.Interval.Contains(safeIntervalEnd) && cell.Interval.Start < safeIntervalEnd) {
                    bottomBounds = cell.Bounds;
                    bottomBounds.Width = aptBounds.Width;
                    bottomBounds.X = aptBounds.X;
                } else if(cell.Interval.Start > safeIntervalStart && cell.Interval.End < safeIntervalEnd) {
                    secondaryBounds.Add(new Rectangle() {
                        X = aptBounds.X,
                        Y = cell.Bounds.Y,
                        Width = aptBounds.Width,
                        Height = cell.Bounds.Height
                    });
                }
                if(generalBounds.X > cell.Bounds.X) {
                    generalBounds.X = cell.Bounds.X;
                }
                if(generalBounds.Y > cell.Bounds.Y) {
                    generalBounds.Y = cell.Bounds.Y;
                }
                if(generalBounds.Width < cell.Bounds.Right) {
                    generalBounds.Width = cell.Bounds.Right - generalBounds.X;
                }
                if(generalBounds.Bottom < cell.Bounds.Bottom) {
                    generalBounds.Height = cell.Bounds.Bottom - generalBounds.Y;
                }
            }
        }
    }
}
