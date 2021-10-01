' 2019.07.12 poprawka - wygasza ProgresRinga po zapisie

Public NotInheritable Class MainPage
    Inherits Page

    Private Sub GuzikiEnable(bIsEnabled As Boolean)
        uiReadDay.IsEnabled = bIsEnabled
        uiReadAll.IsEnabled = bIsEnabled
        uiReadSinceLast.IsEnabled = bIsEnabled
        uiProcesuje.Visibility = If(bIsEnabled, Visibility.Visible, Visibility.Collapsed)
        uiProcesuje.IsActive = Not bIsEnabled
    End Sub

    Private Async Sub uiRUn_Click(sender As Object, e As RoutedEventArgs)
        GuzikiEnable(False)
        Await App.WyciagnijSMS(New Date(1999, 1, 1), False, True, uiMsgCnt)
        uiMsgCnt.Text = ""
        SetLastDate()
        GuzikiEnable(True)
    End Sub

    Private Async Sub uiRunDay_Click(sender As Object, e As RoutedEventArgs)
        GuzikiEnable(False)
        Dim oDate As Date = Date.Now.AddHours(-Date.Now.Hour - 1)
        Await App.WyciagnijSMS(oDate, False, False, uiMsgCnt)
        uiMsgCnt.Text = ""
        GuzikiEnable(True)
    End Sub

    Private Async Sub uiRunSince_Click(sender As Object, e As RoutedEventArgs)
        GuzikiEnable(False)
        Await App.WyciagnijSMS(GetLastDate, False, False, uiMsgCnt)
        uiMsgCnt.Text = ""
        SetLastDate()
        GuzikiEnable(True)
    End Sub

    Private Async Sub uiAutoChange_Toggle(sender As Object, e As RoutedEventArgs)
        App.SetSettingsBool("autobackup", uiSwitch.IsOn)
        If uiSwitch.IsOn Then
            Await App.DodajTriggerPolnocny
        Else
            For Each oTask In Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks
                If oTask.Value.Name = "PKARsmsBackup_Daily" Then oTask.Value.Unregister(True)
            Next
        End If
    End Sub

    Private Sub SetLastDate()
        App.SetSettingsInt("lastYear", Date.Now.Year)
        App.SetSettingsInt("lastMonth", Date.Now.Month)
        App.SetSettingsInt("lastDay", Date.Now.Day)
    End Sub

    Private Function GetLastDate() As Date
        Dim iYr, iMn, iDy As Integer
        iYr = App.GetSettingsInt("lastYear")
        If iYr < 2000 Then
            Return New Date(2000, 1, 1)
        End If
        iMn = App.GetSettingsInt("lastMonth")
        iDy = App.GetSettingsInt("lastDay")

        Return New Date(iYr, iMn, iDy)

    End Function

    Private Sub UstawSince()
        Dim oDate As Date = GetLastDate()

        If oDate.Year < 2001 Then
            uiReadSinceLast.Visibility = Visibility.Collapsed
            Exit Sub
        End If

        uiReadSinceLast.Visibility = Visibility.Visible
        uiReadSinceLast.Content = "Since " & oDate.ToString("yy-MM-dd")

    End Sub

    Private Sub uiGrid_Loaded(sender As Object, e As RoutedEventArgs)
        uiSwitch.IsOn = App.GetSettingsBool("autobackup")
        uiAutoChange_Toggle(Nothing, Nothing)   ' ustawianie triggera
        uiLog.Text = App.GetSettingsString("internalog")

        UstawSince()
    End Sub

    Private Sub uiClearLog_Click(sender As Object, e As RoutedEventArgs)
        App.SetSettingsString("internalog", "reset")
    End Sub

    Private Sub uiShowLog_Click(sender As Object, e As RoutedEventArgs)
        uiLog.Text = App.GetSettingsString("internalog")
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        If App.GetSDcardFolder Is Nothing Then
            uiReadAll.IsEnabled = False
            uiReadSinceLast.IsEnabled = False
            uiReadDay.IsEnabled = False

            uiMsgCnt.Text = "No SD card detected!"
        End If
        Dim dVal As Double
        dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
        uiProcesuje.Width = dVal
        uiProcesuje.Height = dVal

    End Sub
End Class
