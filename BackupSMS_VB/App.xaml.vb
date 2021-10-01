Imports Windows.ApplicationModel.Background
Imports Windows.ApplicationModel.Chat
Imports Windows.ApplicationModel.Contacts
Imports Windows.Storage


'2019.02.19, oRdr.ReadBatchAsync wstawione do TryCatch

''' <summary>
''' Provides application-specific behavior to supplement the default Application class.
''' </summary>
NotInheritable Class App
    Inherits Application

#Region "FromWizard"

    ''' <summary>
    ''' Invoked when the application is launched normally by the end user.  Other entry points
    ''' will be used when the application is launched to open a specific file, to display
    ''' search results, and so forth.
    ''' </summary>
    ''' <param name="e">Details about the launch request and process.</param>
    Protected Overrides Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()

            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed

            If e.PreviousExecutionState = ApplicationExecutionState.Terminated Then
                ' TODO: Load state from previously suspended application
            End If
            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If

        If e.PrelaunchActivated = False Then
            If rootFrame.Content Is Nothing Then
                ' When the navigation stack isn't restored navigate to the first page,
                ' configuring the new page by passing required information as a navigation
                ' parameter
                rootFrame.Navigate(GetType(MainPage), e.Arguments)
            End If

            ' Ensure the current window is active
            Window.Current.Activate()
        End If
    End Sub

    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub
#End Region

#Region "Settings"

    Public Shared Function GetSettingsBool(sName As String, Optional iDefault As Boolean = False) As Boolean
        Dim sTmp As Boolean

        sTmp = iDefault

        If ApplicationData.Current.RoamingSettings.Values.ContainsKey(sName) Then
            sTmp = CBool(ApplicationData.Current.RoamingSettings.Values(sName).ToString)
        End If
        If ApplicationData.Current.LocalSettings.Values.ContainsKey(sName) Then
            sTmp = CBool(ApplicationData.Current.LocalSettings.Values(sName).ToString)
        End If

        Return sTmp

    End Function
    Public Shared Sub SetSettingsBool(sName As String, sValue As Boolean, Optional bRoam As Boolean = False)
        If bRoam Then ApplicationData.Current.RoamingSettings.Values(sName) = sValue.ToString
        ApplicationData.Current.LocalSettings.Values(sName) = sValue.ToString
    End Sub

    Public Shared Function GetSettingsString(sName As String, Optional sDefault As String = "") As String
        Dim sTmp As String

        sTmp = sDefault

        If ApplicationData.Current.RoamingSettings.Values.ContainsKey(sName) Then
            sTmp = ApplicationData.Current.RoamingSettings.Values(sName).ToString
        End If
        If ApplicationData.Current.LocalSettings.Values.ContainsKey(sName) Then
            sTmp = ApplicationData.Current.LocalSettings.Values(sName).ToString
        End If

        Return sTmp

    End Function

    Public Shared Sub SetSettingsString(sName As String, sValue As String, Optional bRoam As Boolean = False)
        If bRoam Then ApplicationData.Current.RoamingSettings.Values(sName) = sValue
        ApplicationData.Current.LocalSettings.Values(sName) = sValue
    End Sub
#Region "Int"
    Public Shared Function GetSettingsInt(sName As String, Optional iDefault As Integer = 0) As Integer
        Dim sTmp As Integer

        sTmp = iDefault

        With Windows.Storage.ApplicationData.Current
            If .RoamingSettings.Values.ContainsKey(sName) Then
                sTmp = CInt(.RoamingSettings.Values(sName).ToString)
            End If
            If .LocalSettings.Values.ContainsKey(sName) Then
                sTmp = CInt(.LocalSettings.Values(sName).ToString)
            End If
        End With

        Return sTmp

    End Function

    Public Shared Sub SetSettingsInt(sName As String, sValue As Integer)
        SetSettingsInt(sName, sValue, False)
    End Sub

    Public Shared Sub SetSettingsInt(sName As String, sValue As Integer, bRoam As Boolean)
        With Windows.Storage.ApplicationData.Current
            If bRoam Then .RoamingSettings.Values(sName) = sValue.ToString
            .LocalSettings.Values(sName) = sValue.ToString
        End With
    End Sub
#End Region

    Private Shared Sub IntLogAppend(sStr As String)
        ' App.SetSettingsString("internalog", App.GetSettingsString("internalog") & vbCrLf & Date.Now.ToString("HH:mm") & " " & sStr)
    End Sub

#End Region

#Region "Trigger"

    Public Shared Async Function DodajTriggerPolnocny() As Task
        IntLogAppend("DodajTriggerPolnocny - START")
        Dim oBAS As BackgroundAccessStatus
        oBAS = Await BackgroundExecutionManager.RequestAccessAsync()

        If oBAS = BackgroundAccessStatus.AlwaysAllowed Or oBAS = BackgroundAccessStatus.AllowedSubjectToSystemPolicy Then
            '    ' https://docs.microsoft.com/en-us/windows/uwp/launch-resume/create-And-register-an-inproc-background-task

            'IntLogAppend("DTS - removing tasks")
            ' po co, skoro OneShot?
            'For Each oTask In BackgroundTaskRegistration.AllTasks
            '    If oTask.Value.Name = "PKARsmsBackup_Daily" Then oTask.Value.Unregister(True)
            'Next

            IntLogAppend("DTS - building task")
            Dim builder As BackgroundTaskBuilder = New BackgroundTaskBuilder
            Dim oRet As BackgroundTaskRegistration

            IntLogAppend("DTS - calculating mins")
            'Dim oDate1 = Date.Now.AddHours(1).AddDays(1)    ' 1h - zeby na nastepną dobę, +1 dzien (bez +1h by trafiał na 20 minut pozniej!)
            'Dim oDate0 = New Date(oDate1.Year, oDate1.Month, oDate1.Day)    ' polnoc
            'oDate0 = oDate0.AddMinutes(-20)
            'Dim oDate0 = New Date(oDate1.Year, oDate1.Month, oDate1.Day)    ' polnoc
            Dim oDateMew As Date
            If Date.Now.Hour > 20 Then
                oDateMew = New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 23, 40, 0).AddDays(1)
            Else
                oDateMew = New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 23, 40, 0) ' DZIS
            End If

            Dim iMin As Integer = (oDateMew - Date.Now).TotalMinutes
            IntLogAppend(" waiting mins:" & iMin)
            'Dim iMin = (24 * 60) - 20    ' 24 godziny po 60 minut bez 20 minut; czyli czas uruchomienia
            'iMin -= Date.Now.Hour() * 60  ' odjąć aktualny czas
            'iMin -= Date.Now.Minute()

            builder.SetTrigger(New TimeTrigger(iMin, True))
            builder.Name = "PKARsmsBackup_Daily"
            oRet = builder.Register()
        Else
            IntLogAppend("DTS - oBAS.Status = " & oBAS.ToString)
        End If

    End Function

    Private moTimerDeferal As BackgroundTaskDeferral = Nothing

    Protected Overrides Async Sub OnBackgroundActivated(args As BackgroundActivatedEventArgs)
        moTimerDeferal = args.TaskInstance.GetDeferral()
        IntLogAppend("OnBackActiv - START")
        Dim oDate As Date = Date.Now.AddHours(-Date.Now.Hour - 1)
        Await WyciagnijSMS(oDate, True, False, Nothing)
        Await DodajTriggerPolnocny()
        moTimerDeferal.Complete()
    End Sub

#End Region

    Private Shared Async Function PhoneNo2ContactName(sPhoneNumber As String) As Task(Of String)
        ' https://stackoverflow.com/questions/34953283/how-to-get-contact-by-phone-number
        If sPhoneNumber = "" Then Return ""
        Try
            Dim oStore As ContactStore = Await Contacts.ContactManager.RequestStoreAsync(ContactStoreAccessType.AllContactsReadOnly)
            Dim oContactRdr As ContactReader = oStore.GetContactReader(New ContactQueryOptions(sPhoneNumber))
            Dim oBatch As ContactBatch = Await oContactRdr.ReadBatchAsync()
            If oBatch.Contacts.Count < 1 Then Return ""
            Return oBatch.Contacts(0).DisplayName
        Catch ex As Exception
            Return "????"
        End Try
    End Function

    Public Shared Async Function GetSDcardFolder() As Task(Of StorageFolder)
        Try
            Dim externalDevices As StorageFolder = KnownFolders.RemovableDevices
            Dim oCards As IReadOnlyList(Of StorageFolder) = Await externalDevices.GetFoldersAsync()
            Return oCards.FirstOrDefault()
        Catch ex As Exception
        End Try

        Return Nothing

    End Function


    Public Shared Async Function WyciagnijSMS(oDate As Date, bInTimer As Boolean, bShowSince As Boolean, uiMsgCnt As TextBlock) As Task
        IntLogAppend("WyciagnijSMS - START")
        'Dim oTextBox As TextBlock = Nothing

        'Try

        '    If Not bInTimer Then
        '        IntLogAppend("WSMS - not timer")
        '        ' znajdz control o nazwie uiMsgCnt
        '        Dim oStackPanel As StackPanel = TryCast(TryCast(TryCast(Window.Current.Content, Frame).Content.Content, Grid).Children(0), StackPanel)
        '        For Each oChld As UIElement In oStackPanel.Children
        '            Dim oTmp As TextBlock = TryCast(oChld, TextBlock)
        '            If oTmp IsNot Nothing Then
        '                If oTmp.Name = "uiMsgCnt" Then
        '                    oTextBox = oTmp
        '                    Exit For
        '                End If
        '            End If
        '        Next
        '    End If
        'Catch ex As Exception
        '    ' w razie błędu oTextBox bedzie = Nothing, ale nie wyleci program
        'End Try

        'If uiProcesuje IsNot Nothing Then
        '    uiProcesuje.Visibility = Visibility.Visible
        '    uiProcesuje.IsActive = True
        'End If


        Dim bError As Boolean = False
        Dim oRdr As ChatMessageReader = Nothing
        Try

            Dim oStore As ChatMessageStore = Await ChatMessageManager.RequestStoreAsync
            IntLogAppend("WSMS - got oStore")

            oRdr = oStore.GetMessageReader
            IntLogAppend("WSMS - got oRdr")
        Catch ex As Exception
            bError = True
        End Try

        If bError Then
            If uiMsgCnt IsNot Nothing Then uiMsgCnt.Text = "ERROR - check permissions?"
            'If uiProcesuje IsNot Nothing Then
            '    uiProcesuje.Visibility = Visibility.Visible
            '    uiProcesuje.IsActive = True
            'End If
            Return
        End If

        Dim sTxt As String = ""
        Dim iGuard As Integer = 0

        Dim iLastRunCnt As Integer = GetSettingsInt("lastRunCnt")
        Dim sLastRun As String = ""
        If bShowSince AndAlso iLastRunCnt > 0 Then sLastRun = " (/" & iLastRunCnt & ")"


        While iGuard < 10000
            iGuard = iGuard + 1
            If uiMsgCnt IsNot Nothing Then uiMsgCnt.Text = iGuard.ToString & sLastRun
            IntLogAppend("WSMS - loop, iGuard=" & iGuard)

            Dim oMsgList As IReadOnlyList(Of ChatMessage)
            Try     ' Try dodane 20190223
                oMsgList = Await oRdr.ReadBatchAsync
            Catch ex As Exception
                If uiMsgCnt IsNot Nothing Then uiMsgCnt.Text = "ERROR - check permissions?"
                'If uiProcesuje IsNot Nothing Then
                '    uiProcesuje.Visibility = Visibility.Visible
                '    uiProcesuje.IsActive = True
                'End If
                Return
            End Try

            If oMsgList.Count < 1 Then Exit While
            ' Folder	From	FromAddress	To	ToAddress	Date	Message
            For Each oMsg As ChatMessage In oMsgList

                If oMsg.IsIncoming Then
                    sTxt = sTxt & "Inbox|"
                Else
                    sTxt = sTxt & "Outbox|"
                End If

                sTxt = sTxt & Await PhoneNo2ContactName(oMsg.From) & "|"   ' from
                sTxt = sTxt & oMsg.From & "|"   ' fromAddress

                Dim sRcptNum As String = ""
                Dim sRcptName As String = ""
                For Each sRcpt As String In oMsg.Recipients
                    sRcptNum = sRcptNum & sRcpt
                    sRcptName = sRcptName & Await PhoneNo2ContactName(sRcpt)
                Next

                sTxt = sTxt & sRcptName & "|"   ' from
                sTxt = sTxt & sRcptNum & "|"   ' fromAddress

                Try ' 20180117: jakby LocalTimeStamp miał być null (np.)...
                    sTxt = sTxt & oMsg.LocalTimestamp.ToString("dd/MM/yyyy HH:mm:ss") & "|"
                Catch ex As Exception
                    sTxt = sTxt & "|"   ' empty date
                End Try

                Try ' 20180117: jakby Body miał być null (np.)...
                    sTxt = sTxt & oMsg.Body
                Catch ex As Exception
                End Try

                ' <Message><Recepients /><Body>A jak chcesz spędzić ten czas.</Body><IsIncoming>true</IsIncoming><IsRead>true</IsRead><Attachments /><LocalTimestamp>131606927899116393</LocalTimestamp><Sender>+48531346962</Sender></Message>

                sTxt &= oMsg.Body & vbCrLf
                If oMsg.LocalTimestamp < oDate Then Exit While
            Next
        End While

        If bShowSince Then SetSettingsInt("lastRunCnt", iGuard)

        If uiMsgCnt IsNot Nothing Then uiMsgCnt.Text = "Saving..."

        Dim sdCard As StorageFolder = Await GetSDcardFolder()

        If sdCard Is Nothing Then
            If uiMsgCnt IsNot Nothing Then uiMsgCnt.Text = "Cannot save - no SD card?"
            'If uiProcesuje IsNot Nothing Then
            '    uiProcesuje.Visibility = Visibility.Visible
            '    uiProcesuje.IsActive = True
            'End If
            Return    ' error - nie ma karty
        End If

        Dim oFold As StorageFolder = Await sdCard.CreateFolderAsync("DataLogs", CreationCollisionOption.OpenIfExists)
        If oFold Is Nothing Then Exit Function
        oFold = Await oFold.CreateFolderAsync("BackupSMS", CreationCollisionOption.OpenIfExists)
        If oFold Is Nothing Then Exit Function
        oFold = Await oFold.CreateFolderAsync(Date.Now.ToString("yyyy"), CreationCollisionOption.OpenIfExists)
        If oFold Is Nothing Then Exit Function
        oFold = Await oFold.CreateFolderAsync(Date.Now.ToString("MM"), CreationCollisionOption.OpenIfExists)
        If oFold Is Nothing Then Exit Function

        Dim sFile As String = "SMS " & Date.Now.ToString("yyyy.MM.dd-HH.mm.ss") & ".csv"
        Dim oFile As StorageFile = Await oFold.CreateFileAsync(sFile, CreationCollisionOption.OpenIfExists)
        Await FileIO.WriteTextAsync(oFile, sTxt)

        If uiMsgCnt IsNot Nothing Then uiMsgCnt.Text = " "

        'If uiProcesuje IsNot Nothing Then
        '    uiProcesuje.Visibility = Visibility.Visible
        '    uiProcesuje.IsActive = True
        'End If
    End Function


End Class
