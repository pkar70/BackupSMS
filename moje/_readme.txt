* pokazywana liczba SMS po export
* nr wersji na ekranie
* migracja do C#, moze do Uno?
* wersja export XML?


========================== MMS	MMS	MMS	MMS	MMS ===============
MMSy:
MMS z samym tekstem.



messageoperatorkind = mms
body jest puste ""
subject = "MMS"


2
?oMsg.Attachments.ElementAt(0)
{Windows.ApplicationModel.Chat.ChatMessageAttachment}
    DataStreamReference: {Windows.Storage.Streams.RandomAccessStreamReference}
    GroupId: 4294967295
    MimeType: "application/smil"
    OriginalFileName: ""
    Text: ""
    Thumbnail: {Windows.Storage.Streams.RandomAccessStreamReference}
    TransferProgress: 0
    Native View: To inspect the native object, enable native code debugging.


?oMsg.Attachments.ElementAt(1)
{Windows.ApplicationModel.Chat.ChatMessageAttachment}
    DataStreamReference: {Windows.Storage.Streams.RandomAccessStreamReference}
    GroupId: 0
    MimeType: "text/plain"
    OriginalFileName: ""
    Text: "a mogłam,mogłam.  jak i najpewniej wieeelu innych :/...  ale przynajmniej  już wiem,ze jest jeszcze coś potencjalnie fajnego do obejrzenia :)...  Ano poszłam byłam,  kuchnia łazieneczka, padłam spać też.  Biorę się teraz do robot roznych,przygotowania na spokojnie  lekcji na skypea,trzeba się skupić, coby się nie denerwować do wieczornego  wyniku  :(.  miłej środy. "
    Thumbnail: {Windows.Storage.Streams.RandomAccessStreamReference}
    TransferProgress: 0
    Native View: To inspect the native object, enable native code debugging.


Dostep do czegokolwiek w ChatMessageAttachment:
This API is not available to all Windows/Windows Phone apps. Unless your developer account is specially provisioned by Microsoft, calls to these APIs will fail at runtime.

SMIL:
"<smil>\r\n<head>\r\n<layout>\r\n<root-layout width=\"240\" height=\"160\" />\r\n<region id=\"Image\" width=\"100%\" height=\"70%\" left=\"0%\" top=\"0%\" fit=\"meet\" />\r\n<region id=\"Text\" width=\"100%\" height=\"30%\" left=\"0%\" top=\"70%\" />\r\n</layout>\r\n</head>\r\n<body>\r\n<par dur=\"5000ms\">\r\n<text src=\"text_0.txt\" region=\"Text\" ></text>\r\n</par>\r\n</body>\r\n</smil>\r\n"
TXT:
"a mogłam,mogłam.  jak i najpewniej wieeelu innych :/...  ale przynajmniej  już wiem,ze jest jeszcze coś potencjalnie fajnego do obejrzenia :)...  Ano poszłam byłam,  kuchnia łazieneczka, padłam spać też.  Biorę się teraz do robot roznych,przygotowania na spokojnie  lekcji na skypea,trzeba się skupić, coby się nie denerwować do wieczornego  wyniku  :(.  miłej środy. "


========================== UNO	UNO	UNO	UNO	UNO ===============


Uno nie ma:
* Background Timer
* Contacts:

linie kodu:
                Windows.ApplicationModel.Contacts.ContactStore oStore = await Windows.ApplicationModel.Contacts.ContactManager.RequestStoreAsync(Windows.ApplicationModel.Contacts.ContactStoreAccessType.AllContactsReadOnly);
                Windows.ApplicationModel.Contacts.ContactReader oContactRdr = oStore.GetContactReader(new Windows.ApplicationModel.Contacts.ContactQueryOptions(sPhoneNumber));
                Windows.ApplicationModel.Contacts.ContactBatch oBatch = await oContactRdr.ReadBatchAsync();
                if (oBatch.Contacts.Count < 1)
                    return "";
                return oBatch.Contacts[0].DisplayName;
metody/struktury:
Windows.ApplicationModel.Contacts.ContactManager.RequestStoreAsync()
Windows.ApplicationModel.Contacts.ContactStoreAccessType.* (0: notimplement, 1: read, 2: write - ale specjalne permission do tego, wiec zalozmy ze unimplemented)
Windows.ApplicationModel.Contacts.ContactQueryOptions (numer telefonu)
oStore.GetContactReader
oContactRdr.ReadBatchAsync - zalozmy, ze wszystko na raz daje (bez stronicowania), albo [jak w Calllog - stronicowanie]
oBatch.Contacts
oBatch.Contacts[0].DisplayName

mozna rozszerzyc UWP: ContactQueryOptions(numer, pola) - Phone=4 (bit mask, all, email, name, none, phone)


https://stackoverflow.com/questions/2356084/read-all-contacts-phone-numbers-in-android
https://developer.android.com/reference/android/provider/ContactsContract.html

https://developer.android.com/reference/android/provider/ContactsContract.Contacts.html
If you need to look up a contact by the phone number, use PhoneLookup#CONTENT_FILTER_URI, which is optimized for this purpose
- wedle PHONE, ale to jest tylko readonly

https://developer.android.com/reference/android/provider/ContactsContract.PhoneLookup.html#CONTENT_FILTER_URI

If you need to look up a contact by partial name, e.g. to produce filter-as-you-type suggestions, use the CONTENT_FILTER_URI URI.
- wedle NAME

https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.contacts.contact
Addresses
DisplayName - ze tylko z UI thread? a przeciez chyba takze z Timer?
chyba ze to zmienili i dlatego nie dziala?
Emails
FirstName
FullName
ale ze DispName = First, Middle, Last, a to jest zawsze?


* zapisywanie do pliku
Windows.Storage.KnownFolders.RemovableDevices
externalDevices.GetFoldersAsync()

* SMS
Windows.ApplicationModel.Chat.ChatMessageStore oStore = await Windows.ApplicationModel.Chat.ChatMessageManager.RequestStoreAsync();
oStore.GetMessageReader()
oRdr.ReadBatchAsync()
oMsg.IsIncoming
oMsg.From
oMsg.LocalTimestamp

* import
Windows.Storage.Pickers.FileOpenPicker()
picker.FileTypeFilter
picker.PickSingleFileAsync()
Windows.Storage.FileIO.ReadTextAsync

Windows.ApplicationModel.Chat.ChatMessage(); - JEST
oMsg.MessageKind
Windows.ApplicationModel.Chat.ChatMessageKind.*
oMsg.MessageOperatorKind
Windows.ApplicationModel.Chat.ChatMessageOperatorKind.*
oMsg.IsRead
oMsg.IsSeen
oMsg.Status
(ale: Windows.ApplicationModel.Chat.ChatMessageStatus.Received - jest!)
oMsg.LocalTimestamp
oMsg.NetworkTimestamp
oStore.SaveMessageAsync


oUri = Android.Provider.ContactsContract.Data.ContentUri;

count=7893
columncount=89


    [0]: "phonetic_name"	null
    [1]: "status_res_package"
    [2]: "custom_ringtone"
    [3]: "contact_status_ts"
    [4]: "account_type"		string. com.google
    [5]: "data_version"
    [6]: "photo_file_id"
    [7]: "contact_status_res_package"
    [8]: "group_sourceid"
    [9]: "indicate_phone_or_sim_contact"
    [10]: "display_name_alt"
    [11]: "sort_key_alt"
    [12]: "mode"
    [13]: "last_time_used"
    [14]: "starred"
    [15]: "contact_status_label"
    [16]: "has_phone_number"	int = 1
    [17]: "chat_capability"
    [18]: "raw_contact_id"
    [19]: "carrier_presence"
    [20]: "contact_last_updated_timestamp"
    [21]: "res_package"
    [22]: "photo_uri"
    [23]: "data_sync4"
    [24]: "phonebook_bucket"
    [25]: "times_used"
    [26]: "display_name"	= Teresa
    [27]: "sort_key"		string = Teresa
    [28]: "send_to_voicemail_vt"
    [29]: "data_sync1"
    [30]: "version"
    [31]: "data_sync2"
    [32]: "data_sync3"
    [33]: "photo_thumb_uri"
    [34]: "is_additional_number"
    [35]: "status_label"
    [36]: "contact_presence"
    [37]: "in_default_directory"
    [38]: "times_contacted"
    [39]: "_id"
    [40]: "account_type_and_data_set"
    [41]: "name_raw_contact_id"	int
    [42]: "status"
    [43]: "phonebook_bucket_alt"
    [44]: "last_time_contacted"
    [45]: "pinned"
    [46]: "is_primary"
    [47]: "photo_id"
    [48]: "send_to_voicemail_sip"
    [49]: "contact_id"
    [50]: "contact_chat_capability"
    [51]: "contact_status_icon"
    [52]: "is_sdn_contact"
    [53]: "in_visible_group"
    [54]: "phonebook_label"	string = T
    [55]: "account_name"	string = pkar@ieee.org
    [56]: "display_name_source"
    [57]: "timestamp"
    [58]: "data9"
    [59]: "dirty"
    [60]: "sourceid"
    [61]: "phonetic_name_style"
    [62]: "send_to_voicemail"
    [63]: "data8"
    [64]: "lookup"
    [65]: "data7"
    [66]: "data6"
    [67]: "phonebook_label_alt"
    [68]: "data5"
    [69]: "is_super_primary"
    [70]: "data4"		<- normalized number [API16]
    [71]: "data3"
    [72]: "data2"		<- ze typ numeru
    [73]: "data1"		<- ze number?
    [74]: "data_set"
    [75]: "contact_status"
    [76]: "backup_id"
    [77]: "raw_contact_is_user_profile"
    [78]: "status_ts"
    [79]: "data10"
    [80]: "data12"
    [81]: "mimetype"
    [82]: "status_icon"
    [83]: "data11"
    [84]: "data14"
    [85]: "index_in_sim"
    [86]: "data13"
    [87]: "hash_id"
    [88]: "data15"


oUri = Android.Provider.ContactsContract.Contacts.ContentFilterUri; (ale zero znalezionych)

    [0]: "last_time_contacted"
    [1]: "phonetic_name"
    [2]: "snippet"
    [3]: "custom_ringtone"
    [4]: "contact_status_ts"
    [5]: "pinned"
    [6]: "photo_id"
    [7]: "photo_file_id"
    [8]: "contact_status_res_package"
    [9]: "indicate_phone_or_sim_contact"
    [10]: "send_to_voicemail_sip"
    [11]: "contact_chat_capability"
    [12]: "contact_status_icon"
    [13]: "display_name_alt"
    [14]: "sort_key_alt"
    [15]: "in_visible_group"
    [16]: "is_sdn_contact"
    [17]: "starred"
    [18]: "contact_status_label"
    [19]: "phonebook_label"
    [20]: "is_user_profile"
    [21]: "has_phone_number"
    [22]: "display_name_source"
    [23]: "phonetic_name_style"
    [24]: "send_to_voicemail"
    [25]: "lookup"
    [26]: "phonebook_label_alt"
    [27]: "contact_last_updated_timestamp"
    [28]: "photo_uri"
    [29]: "phonebook_bucket"
    [30]: "contact_status"
    [31]: "display_name"
    [32]: "sort_key"
    [33]: "send_to_voicemail_vt"
    [34]: "photo_thumb_uri"
    [35]: "contact_presence"
    [36]: "in_default_directory"
    [37]: "times_contacted"
    [38]: "_id"
    [39]: "index_in_sim"
    [40]: "name_raw_contact_id"
    [41]: "phonebook_bucket_alt"


oUri = Android.Provider.ContactsContract.PhoneLookup.ContentFilterUri;
    [0]: "phonetic_name"
    [1]: "last_time_contacted"
    [2]: "custom_ringtone"
    [3]: "photo_id"
    [4]: "photo_file_id"
    [5]: "indicate_phone_or_sim_contact"
    [6]: "send_to_voicemail_sip"
    [7]: "contact_id"
    [8]: "type"
    [9]: "display_name_alt"
    [10]: "sort_key_alt"
    [11]: "number"
    [12]: "normalized_number"
    [13]: "in_visible_group"
    [14]: "is_sdn_contact"
    [15]: "starred"
    [16]: "data_id"
    [17]: "display_name_source"
    [18]: "has_phone_number"
    [19]: "phonetic_name_style"
    [20]: "send_to_voicemail"
    [21]: "raw_contact_id"
    [22]: "lookup"
    [23]: "data4"
    [24]: "photo_uri"
    [25]: "data3"
    [26]: "data1"
    [27]: "contact_status"
    [28]: "label"
    [29]: "display_name"
    [30]: "sort_key"
    [31]: "send_to_voicemail_vt"
    [32]: "photo_thumb_uri"
    [33]: "filter"
    [34]: "contact_presence"
    [35]: "times_contacted"
    [36]: "in_default_directory"
    [37]: "_id"
    [38]: "index_in_sim"
