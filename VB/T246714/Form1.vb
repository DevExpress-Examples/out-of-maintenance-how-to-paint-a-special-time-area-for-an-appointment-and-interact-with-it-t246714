Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports DevExpress.Utils.Drawing
Imports DevExpress.XtraScheduler
Imports DevExpress.XtraScheduler.Drawing

Namespace XtraSchedulerAppAndRes
	Partial Public Class Form1
		Inherits Form

		Private drag As Boolean = False

		Public Sub New()
			InitializeComponent()
			schedulerControl1.GroupType = SchedulerGroupType.Resource
			schedulerControl1.OptionsCustomization.AllowAppointmentMultiSelect = False
			schedulerControl1.Start = Date.Today
			InitializeStorage()
			AddHandler schedulerControl1.CustomDrawAppointment, AddressOf schedulerControl1_CustomDrawAppointment
			AddHandler schedulerControl1.AppointmentDrag, AddressOf schedulerControl1_AppointmentDrag
			AddHandler schedulerControl1.AppointmentDrop, AddressOf schedulerControl1_AppointmentDrop
		End Sub

		Private Sub schedulerControl1_AppointmentDrop(ByVal sender As Object, ByVal e As AppointmentDragEventArgs)
			drag = False
		End Sub

		Private Sub InitializeStorage()
			Dim safeIntervalStartCustomField As New AppointmentCustomFieldMapping("SafeIntervalStartCF", "SafeIntervalStartDB", FieldValueType.DateTime)
			schedulerStorage1.Appointments.CustomFieldMappings.Add(safeIntervalStartCustomField)
			Dim safeIntervalEndCustomField As New AppointmentCustomFieldMapping("SafeIntervalEndCF", "SafeIntervalEndDB", FieldValueType.DateTime)
			schedulerStorage1.Appointments.CustomFieldMappings.Add(safeIntervalEndCustomField)

			Dim apt1 As Appointment = schedulerStorage1.CreateAppointment(AppointmentType.Normal)
			apt1.Subject = "TestSubject1"
            apt1.Start = Date.Today.AddHours(1)
			apt1.Duration = TimeSpan.FromHours(1)
			apt1.CustomFields("SafeIntervalStartCF") = apt1.Start.AddHours(-1)
			apt1.CustomFields("SafeIntervalEndCF") = apt1.End.AddHours(1)
			schedulerStorage1.Appointments.Add(apt1)

			Dim apt2 As Appointment = schedulerStorage1.CreateAppointment(AppointmentType.Normal)
			apt2.Subject = "TestSubject2"
			apt2.Start = Date.Today.AddHours(5)
			apt2.Duration = TimeSpan.FromHours(1)
			apt2.CustomFields("SafeIntervalStartCF") = apt2.Start.AddHours(-1)
			apt2.CustomFields("SafeIntervalEndCF") = apt2.End.AddHours(1)
			schedulerStorage1.Appointments.Add(apt2)

			AddHandler schedulerStorage1.AppointmentInserting, AddressOf schedulerStorage1_AppointmentInserting
			AddHandler schedulerStorage1.AppointmentChanging, AddressOf schedulerStorage1_AppointmentChanging
		End Sub

		Private Sub schedulerStorage1_AppointmentChanging(ByVal sender As Object, ByVal e As PersistentObjectCancelEventArgs)
			Dim apt As Appointment = CType(e.Object, Appointment)
			e.Cancel = (apt.Start < DirectCast(apt.CustomFields("SafeIntervalStartCF"), Date) OrElse apt.End > DirectCast(apt.CustomFields("SafeIntervalEndCF"), Date))
		End Sub

		Private Sub schedulerStorage1_AppointmentInserting(ByVal sender As Object, ByVal e As PersistentObjectCancelEventArgs)
			 Dim apt As Appointment = CType(e.Object, Appointment)
			apt.CustomFields("SafeIntervalStartCF") = apt.Start.AddHours(-1)
			apt.CustomFields("SafeIntervalEndCF") = apt.End.AddHours(1)
		End Sub

		Private Sub schedulerControl1_AppointmentDrag(ByVal sender As Object, ByVal e As AppointmentDragEventArgs)
			Dim scheduler As SchedulerControl = DirectCast(sender, SchedulerControl)
			Dim apt As Appointment = e.SourceAppointment
			Dim safeIntervalStart As Date = DirectCast(apt.CustomFields("SafeIntervalStartCF"), Date)
			Dim safeIntervalEnd As Date = DirectCast(apt.CustomFields("SafeIntervalEndCF"), Date)
			e.Allow = e.EditedAppointment.Start >= safeIntervalStart AndAlso e.EditedAppointment.End <= safeIntervalEnd
			drag = True
		End Sub

		Private Sub schedulerControl1_CustomDrawAppointment(ByVal sender As Object, ByVal e As CustomDrawObjectEventArgs)
			Dim scheduler As SchedulerControl = DirectCast(sender, SchedulerControl)
			If scheduler.ActiveViewType = SchedulerViewType.Day Then
				Dim aptViewInfo As AppointmentViewInfo = CType(e.ObjectInfo, AppointmentViewInfo)
				If aptViewInfo.Selected Then
					Dim apt As Appointment = aptViewInfo.Appointment
					Dim brush As New SolidBrush((If(drag, Color.FromArgb(32, 0, 0, 0), Color.FromArgb(16, 0, 0, 0))))
					Dim timeScale As TimeSpan = scheduler.DayView.TimeScale
					Dim safeIntervalStart As Date = DirectCast(apt.CustomFields("SafeIntervalStartCF"), Date)
					Dim safeIntervalEnd As Date = DirectCast(apt.CustomFields("SafeIntervalEndCF"), Date)
					Dim cellsCount As Integer = CInt((safeIntervalEnd.Subtract(safeIntervalStart)).Ticks \ timeScale.Ticks)
					Dim cells As SchedulerViewCellBaseCollection = scheduler.DayView.ViewInfo.CellContainers(0).Cells
					Dim topBounds As Rectangle = Rectangle.Empty
					Dim bottomBounds As Rectangle = Rectangle.Empty
                    Dim secondaryBounds As New List(Of Rectangle)()
                    Dim generalBounds As Rectangle = Rectangle.Empty
                    InitializeBoundsInfo(safeIntervalStart, safeIntervalEnd, cells, topBounds, bottomBounds, secondaryBounds, generalBounds, aptViewInfo.Bounds)
                    Draw(e, brush, topBounds, bottomBounds, secondaryBounds, generalBounds)
				End If
			End If
		End Sub

        Private Sub Draw(ByVal e As CustomDrawObjectEventArgs, ByVal brush As SolidBrush, ByRef topBounds As Rectangle, ByRef bottomBounds As Rectangle, ByVal secondaryBounds As List(Of Rectangle), ByRef generalBounds As Rectangle)
            Dim clip As Region = e.Graphics.Clip
            e.DrawDefault()
            Dim newClip As New Region(generalBounds)
            e.Graphics.Clip = newClip
            e.Graphics.FillRectangle(brush, topBounds)
            e.Graphics.DrawLines(New Pen(Color.Black, 1), New Point() {
                New Point(topBounds.X, topBounds.Y + topBounds.Height - 1),
                New Point(topBounds.X, topBounds.Y),
                New Point(topBounds.X + topBounds.Width - 1, topBounds.Y),
                New Point(topBounds.X + topBounds.Width - 1, topBounds.Y + topBounds.Height - 1)
            })
            For Each r As Rectangle In secondaryBounds
                e.Graphics.FillRectangle(brush, r)
                e.Graphics.DrawLines(New Pen(Color.Black, 1), New Point() {
                    New Point(r.X, r.Y),
                    New Point(r.X, r.Y + r.Height - 1)
                })
                e.Graphics.DrawLines(New Pen(Color.Black, 1), New Point() {
                    New Point(r.X + r.Width - 1, r.Y),
                    New Point(r.X + r.Width - 1, r.Y + r.Height - 1)
                })
            Next r
            e.Graphics.FillRectangle(brush, bottomBounds)
            e.Graphics.DrawLines(New Pen(Color.Black, 1), New Point() {
                New Point(bottomBounds.X, bottomBounds.Y),
                New Point(bottomBounds.X, bottomBounds.Y + bottomBounds.Height - 1),
                New Point(bottomBounds.X + bottomBounds.Width - 1, bottomBounds.Y + bottomBounds.Height - 1),
                New Point(bottomBounds.X + bottomBounds.Width - 1, bottomBounds.Y)
            })
            e.Graphics.Clip = clip
            e.Handled = True
        End Sub

        Private Shared Sub InitializeBoundsInfo(ByVal safeIntervalStart As Date, ByVal safeIntervalEnd As Date, ByVal cells As SchedulerViewCellBaseCollection, ByRef topBounds As Rectangle, ByRef bottomBounds As Rectangle, ByVal secondaryBounds As List(Of Rectangle), ByRef generalBounds As Rectangle, ByVal aptBounds As Rectangle)
            For i As Integer = 0 To cells.Count - 1
                Dim cell As SchedulerViewCellBase = cells(i)
                If cell.Interval.Contains(safeIntervalStart) AndAlso cell.Interval.End > safeIntervalStart Then
                    topBounds = cell.Bounds
                    topBounds.Width = aptBounds.Width
                    topBounds.X = aptBounds.X
                ElseIf cell.Interval.Contains(safeIntervalEnd) AndAlso cell.Interval.Start < safeIntervalEnd Then
                    bottomBounds = cell.Bounds
                    bottomBounds.Width = aptBounds.Width
                    bottomBounds.X = aptBounds.X
                ElseIf cell.Interval.Start > safeIntervalStart AndAlso cell.Interval.End < safeIntervalEnd Then
                    secondaryBounds.Add(New Rectangle() With {.X = aptBounds.X, .Y = cell.Bounds.Y, .Width = aptBounds.Width, .Height = cell.Bounds.Height})
                End If
                If generalBounds.X > cell.Bounds.X Then
                    generalBounds.X = cell.Bounds.X
                End If
                If generalBounds.Y > cell.Bounds.Y Then
                    generalBounds.Y = cell.Bounds.Y
                End If
                If generalBounds.Width < cell.Bounds.Right Then
                    generalBounds.Width = cell.Bounds.Right - generalBounds.X
                End If
                If generalBounds.Bottom < cell.Bounds.Bottom Then
                    generalBounds.Height = cell.Bounds.Bottom - generalBounds.Y
                End If
            Next i
        End Sub
	End Class
End Namespace
