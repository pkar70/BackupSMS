
// test, zanim to trafi do Uno

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Linq;

namespace BeforeUno
{
	#region "myDumps"

	class MyDUmps
	{
		static bool _bWasTypes = false;

		public static string DumpTableHeaderNames(Android.Database.ICursor _cursor)
		{
			string sDump = "";
			if (_cursor != null)
			{
				sDump = sDump + "Row count: " + _cursor.Count.ToString() + "\n";
				sDump = sDump + "Column count: " + _cursor.ColumnCount.ToString() + "\n\n";
				for (int i = 0; i < _cursor.ColumnCount; i++)
				{
					sDump = sDump + "|" + _cursor.GetColumnName(i);
				}
				sDump = sDump + "\n";
				//for (int i = 0; i < _cursor.ColumnCount; i++)
				//{
				//	sDump = sDump + _cursor.GetType(i);
				//}
				//sDump = sDump + "\n";
			}
			_bWasTypes = false;
			return sDump;
		}

		public static string DumpTableHeaderTypes(Android.Database.ICursor _cursor)
		{
			string sDump = "";

			if (_cursor.IsBeforeFirst)
				return sDump;   // jeszcze nie mozemy

			for (int i = 0; i < _cursor.ColumnCount; i++)
			{
				switch(_cursor.GetType(i))
				{
					case Android.Database.FieldType.Blob:
						sDump = sDump + "|blob";
						break;
					case Android.Database.FieldType.Float:
						sDump = sDump + "|float";
						break;
					case Android.Database.FieldType.Integer:
						sDump = sDump + "|int";
						break;
					case Android.Database.FieldType.Null :
						sDump = sDump + "|null";
						break;
					case Android.Database.FieldType.String :
						sDump = sDump + "|string";
						break;
					default:
						sDump = sDump + "|UNKNOWN";
						break;
				}
			}
			sDump = sDump + "\n\n";


			_bWasTypes = true;
			return sDump;
		}
		public static string DumpTableRows(Android.Database.ICursor _cursor)
		{
			string sDump = "";

			for (int pageGuard = 100; pageGuard > 0 && _cursor.MoveToNext(); pageGuard--)
			{

				if (!_bWasTypes)
					sDump = DumpTableHeaderTypes(_cursor);

				for (int i = 0; i < _cursor.ColumnCount; i++)
				{
					switch (_cursor.GetType(i))
					{
						case Android.Database.FieldType.Blob:
							sDump = sDump + "|<blob>";
							break;
						case Android.Database.FieldType.Float:
							try
							{
								sDump = sDump + "|" + _cursor.GetFloat(i).ToString();
							}
							catch
							{
								sDump = sDump + "|<error>";
							}
							break;
						case Android.Database.FieldType.Integer:
							try
							{ 
							sDump = sDump + "|" + _cursor.GetInt(i).ToString();
							}
							catch
							{
								sDump = sDump + "|<error>";
							}

							break;
						case Android.Database.FieldType.Null:
							sDump = sDump + "|null";
							break;
						case Android.Database.FieldType.String:
							try
							{ 
							sDump = sDump + "|" + _cursor.GetString(i);
							}
							catch
							{
								sDump = sDump + "|<error>";
							}

							break;
						default:
							sDump = sDump + "|UNKNOWN";
							break;
					}
				}
				sDump = sDump + "\n";

			}

			return sDump;

		}

	}
	#endregion

	#region "contact"
	public enum ContactStoreAccessType
	{
		AppContactsReadWrite,
		AllContactsReadOnly,
		AllContactsReadWrite,
	}

	public partial class ContactManager
	{

		public static IAsyncOperation<ContactStore> RequestStoreAsync() => RequestStoreAsync(ContactStoreAccessType.AllContactsReadOnly);

		public static IAsyncOperation<ContactStore> RequestStoreAsync(ContactStoreAccessType accessType) => RequestStoreAsyncTask(accessType).AsAsyncOperation<ContactStore>();
		//{
		//	return RequestStoreAsyncTask(accessType).AsAsyncOperation<ContactStore>();
		//}

		private static async Task<ContactStore> RequestStoreAsyncTask(ContactStoreAccessType accessType)
		{
			// UWP: AppContactsReadWrite, AllContactsReadOnly, AllContactsReadWrite (cannot be used without special provisioning by Microsoft)
			// Android: Manifest has READ_CONTACTS and WRITE_CONTACTS, no difference between app/limited/full
			// using only AllContactsReadOnly, other: throw NotImplementedException

			if (accessType != ContactStoreAccessType.AllContactsReadOnly)
				throw new NotImplementedException();

			// do we have declared this permission in Manifest?
			// it could be also Coarse, without GPS
			Android.Content.Context context = Android.App.Application.Context;
			Android.Content.PM.PackageInfo packageInfo =
				context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
			var requestedPermissions = packageInfo?.RequestedPermissions;
			if (requestedPermissions is null)
				return null;

			bool bInManifest = requestedPermissions.Any(p => p.Equals(Android.Manifest.Permission.ReadContacts, StringComparison.OrdinalIgnoreCase));// false;
			//bool bInManifest = false; // 
			//foreach (string oPerm in requestedPermissions)
			//{
			//	if (oPerm.Equals(Android.Manifest.Permission.ReadContacts, StringComparison.OrdinalIgnoreCase))
			//		bInManifest = true;
			//}

			if (!bInManifest)
				return null;


			// check if permission is granted
			if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.ReadContacts)
					== Android.Content.PM.Permission.Granted)
			{
				return new ContactStore();
			}


			// system dialog asking for permission

			// this code would not compile here - but it compile in your own app.
			// to be compiled inside Uno, it has to be splitted into layers
			var tcs = new TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

			void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
			{

				if (e.RequestCode == 1)
				{
					tcs.TrySetResult(e);
				}
			}

			var current = Uno.UI.BaseActivity.Current;

			try
			{
				current.RequestPermissionsResultWithResults += handler;

				Android.Support.V4.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, 
					new[] { Android.Manifest.Permission.ReadContacts }, 1);

				var result = await tcs.Task;
				if (result.GrantResults.Length < 1)
					return null;
				if (result.GrantResults[0] == Android.Content.PM.Permission.Granted)
					return new ContactStore();

			}
			finally
			{
				current.RequestPermissionsResultWithResults -= handler;
			}

			return null;

		}

	}

	public partial class ContactStore
	{
		
		public ContactReader GetContactReader() => GetContactReader(new ContactQueryOptions("", ContactQuerySearchFields.None));

		public ContactReader GetContactReader(ContactQueryOptions options)
		{
			return new ContactReader(options);
		}

	}

	[Flags]
	public enum ContactQuerySearchFields
	{
		None = 0,   // no search - all entries
		Name = 1,
		Email = 2,
		Phone = 4,
		All = -1 // 4294967295 == 0b_1111_1111_1111_1111_1111_1111_1111_1111 == ‭FFFFFFFF‬
	}

	[Flags]
	public enum ContactQueryDesiredFields
	{
		None = 0,
		PhoneNumber = 1,
		EmailAddress = 2,
		PostalAddress = 4
	}

	public partial class ContactQueryOptions
	{   // https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.contacts.contactqueryoptions.-ctor

		internal ContactQuerySearchFields _whereToSearch;
		internal string _whatToSearch;

		public ContactQueryDesiredFields DesiredFields { get; set; }

		public ContactQueryOptions(string text) // => ContactQueryOptions(text, ContactQuerySearchFields.All);
		{
			_whatToSearch = text;
			_whereToSearch = ContactQuerySearchFields.All;
		}

		public ContactQueryOptions(string text, ContactQuerySearchFields fields)
		{
			_whatToSearch = text;
			_whereToSearch = fields;
		}

		public ContactQueryOptions()
		{
			_whatToSearch = "";
			_whereToSearch = ContactQuerySearchFields.None;
		}

	}

	public partial class ContactReader
	{
		internal ContactQueryOptions _queryOptions;
		private Android.Database.ICursor _cursor = null;
		private Android.Content.ContentResolver _contentResolver = null;

		internal ContactReader(ContactQueryOptions options)
		{
			if (options is null)
				throw new ArgumentNullException();

			_queryOptions = options;

			Android.Net.Uri oUri;
			string sColumnIdName = "_id";

			switch(options._whereToSearch)
			{
				case ContactQuerySearchFields.Phone:
					oUri = Android.Net.Uri.WithAppendedPath(
						Android.Provider.ContactsContract.PhoneLookup.ContentFilterUri, // jego Phone.Contact_ID to .Contacts._ID 
						Android.Net.Uri.Encode(options._whatToSearch));
					sColumnIdName = "contact_id";
					break;
				case ContactQuerySearchFields.Name:
					oUri = Android.Net.Uri.WithAppendedPath(
						Android.Provider.ContactsContract.Contacts.ContentFilterUri,
						Android.Net.Uri.Encode(options._whatToSearch)); 
					break;
				default:
					oUri = Android.Provider.ContactsContract.Contacts.ContentUri; // ich _ID == Phone.Contact_ID
					break;
			}
			
			// filtr moglby byc... ale wedle czego?
			// ewentualnie wedle substring DisplayName

			_contentResolver = Android.App.Application.Context.ContentResolver;

			_cursor = _contentResolver.Query(oUri,
									new string[] { sColumnIdName, "display_name" },  // which columns
									null,	// where
									null,	// null
									null);   // == date DESC

		}

		public IAsyncOperation<ContactBatch> ReadBatchAsync()
		{
			ContactBatch batch = new ContactBatch(ReadBatchInternal());
			return null;
		}

		internal List<Contact> ReadBatchInternal()
		{
			var entriesList = new List<Contact>();

			if (_cursor is null)
			{
				return entriesList;
			}


			ContactQueryDesiredFields desiredFields = _queryOptions.DesiredFields;
			// default value (==None) treat as "all"
			if (desiredFields == ContactQueryDesiredFields.None)
				desiredFields = ContactQueryDesiredFields.EmailAddress | ContactQueryDesiredFields.PhoneNumber | ContactQueryDesiredFields.PostalAddress;

			// add fields we search by
			if (_queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Email))
				desiredFields |= ContactQueryDesiredFields.EmailAddress;
			if (_queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Phone))
				desiredFields |= ContactQueryDesiredFields.PhoneNumber;

			for (int pageGuard = 100; pageGuard > 0 && _cursor.MoveToNext(); pageGuard--)
			{
				var entry = new Contact();
				int contactId = _cursor.GetInt(0);  // we defined columns while opening cursor, so we know what data is in which columns

				entry.DisplayName = _cursor.GetString(1);   // we defined columns while opening cursor, so we know what data is in which columns

				bool searchFound = false; // should it be included in result set
				if (_queryOptions._whereToSearch == ContactQuerySearchFields.None ||	// no filtering at all
					_queryOptions._whereToSearch == ContactQuerySearchFields.Phone ||	// filtering done by Android
					_queryOptions._whereToSearch == ContactQuerySearchFields.Name)      // filtering done by Android
							searchFound = true; // include in result - and skip tests

				if (!searchFound && _queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Name))
				{
					if (entry.DisplayName.Contains(_queryOptions._whatToSearch))
						searchFound = true;
				}



				// filling properties, using other tables


				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.Phone
				// NUMBER, TYPE
				entry.Phones.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PhoneNumber))
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
										Android.Provider.ContactsContract.Data.ContentUri,
										new string[] { "data1", "data2" }, //null,   // all columns
																		   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
										"contact_id = ? AND mimetype = ?",
										new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.Phone.ContentItemType },
										null);   // default order

					//int columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // Phone.NUMBER
					//int columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // Phone.TYPE

					for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
					{
						var itemEntry = new ContactPhone();
						itemEntry.Number = subCursor.GetString(0);   // we defined columns while opening cursor, so we know what data is in which columns

						if (!searchFound && _queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Phone))
						{
							if (itemEntry.Number.Contains(_queryOptions._whatToSearch))
								searchFound = true;
						}

						switch (subCursor.GetInt(1))    // we defined columns while opening cursor, so we know what data is in which columns
						{
							case 1:
								itemEntry.Kind = ContactPhoneKind.Home;
								break;
							case 2:
								itemEntry.Kind = ContactPhoneKind.Mobile;
								break;
							case 3:
								itemEntry.Kind = ContactPhoneKind.Work;
								break;
							case 6:
								itemEntry.Kind = ContactPhoneKind.Pager;
								break;
							case 4:
								itemEntry.Kind = ContactPhoneKind.BusinessFax;
								break;
							case 5:
								itemEntry.Kind = ContactPhoneKind.HomeFax;
								break;
							case 10:
								itemEntry.Kind = ContactPhoneKind.Company;
								break;
							case 19:
								itemEntry.Kind = ContactPhoneKind.Assistant;
								break;
							case 14:
								itemEntry.Kind = ContactPhoneKind.Radio;
								break;
							default:    // TYPE_CALLBACK, TYPE_CAR, TYPE_ISDN, TYPE_MAIN, TYPE_MMS, TYPE_OTHER, TYPE_OTHER_FAX, TYPE_PAGER, TYPE_TELEX, TYPE_TTY_TDD, TYPE_WORK_MOBILE, TYPE_WORK_PAGER
								itemEntry.Kind = ContactPhoneKind.Other;
								break;
						}
						entry.Phones.Add(itemEntry);

					}
					subCursor.Close();


					if (!searchFound && _queryOptions._whereToSearch == ContactQuerySearchFields.Phone)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}

				}

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.Email
				// ADDRESS, TYPE
				entry.Emails.Clear();

				if (desiredFields.HasFlag(ContactQueryDesiredFields.EmailAddress))
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
									Android.Provider.ContactsContract.Data.ContentUri,
									new string[] { "data1", "data2" }, //null,   // all columns
																	   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
									"contact_id = ? AND mimetype = ?",
									new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.Email.ContentItemType },
									null);   // default order

					//columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // Email.ADDRESS
					//columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // Email.TYPE
					for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
					{
						var itemEntry = new ContactEmail();
						itemEntry.Address = subCursor.GetString(0);     // we defined columns while opening cursor, so we know what data is in which columns
						if (!searchFound && _queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Email))
						{
							if (itemEntry.Address.Contains(_queryOptions._whatToSearch))
								searchFound = true;
						}

						switch (subCursor.GetInt(1))    // we defined columns while opening cursor, so we know what data is in which columns
						{
							case 1: // TYPE_HOME
								itemEntry.Kind = ContactEmailKind.Personal;
								break;
							case 2:
								itemEntry.Kind = ContactEmailKind.Work;
								break;
							default:    // TYPE_MOBILE, TYPE_OTHER
								itemEntry.Kind = ContactEmailKind.Other;
								break;
						}
						entry.Emails.Add(itemEntry);
					}
					subCursor.Close();

					if (!searchFound && _queryOptions._whereToSearch == ContactQuerySearchFields.Email)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredName
				// DISPLAY_NAME, GIVEN_NAME, FAMILY_NAME, PREFIX, MIDDLE_NAME, SUFFIX, PHONETIC_GIVEN_NAME, PHONETIC_MIDDLE_NAME, PHONETIC_FAMILY_NAME
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
										Android.Provider.ContactsContract.Data.ContentUri,
										new string[] { "data1", "data2", "data3", "data4", "data5", "data6" }, //null,   // all columns
																											   // ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
										"contact_id = ? AND mimetype = ?",
										new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.StructuredName.ContentItemType },
										null);   // default order

					//columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // DISPLAY_NAME
					//columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // 	GIVEN_NAME
					//int columnD3 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data3); // 	FAMILY_NAME
					//int columnD4 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data4); // 	PREFIX
					//int columnD5 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data5); // 	MIDDLE_NAME
					//int columnD6 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data6); // 	SUFFIX
					//int columnD7 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data7); // 	PHONETIC_GIVEN_NAME
					//int columnD8 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data8); // 	PHONETIC_MIDDLE_NAME
					//int columnD9 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data9); // 	PHONETIC_FAMILY_NAME

					if (subCursor.MoveToNext())
					{

						// entry.FullName { get; internal set; }
						entry.MiddleName = subCursor.GetString(4);       // we defined columns while opening cursor, so we know what data is in which columns
						entry.LastName = subCursor.GetString(2);         // we defined columns while opening cursor, so we know what data is in which columns
						entry.FirstName = subCursor.GetString(1);        // we defined columns while opening cursor, so we know what data is in which columns
						entry.HonorificNamePrefix = subCursor.GetString(3);  // we defined columns while opening cursor, so we know what data is in which columns
						entry.HonorificNameSufix = subCursor.GetString(5);   // we defined columns while opening cursor, so we know what data is in which columns
						entry.DisplayName = subCursor.GetString(1);      // we defined columns while opening cursor, so we know what data is in which columns

						if (!searchFound && _queryOptions._whereToSearch.HasFlag(ContactQuerySearchFields.Name))
						{
							if (entry.MiddleName.Contains(_queryOptions._whatToSearch) ||
									entry.LastName.Contains(_queryOptions._whatToSearch) ||
									entry.FirstName.Contains(_queryOptions._whatToSearch) ||
									entry.HonorificNamePrefix.Contains(_queryOptions._whatToSearch) ||
									entry.HonorificNameSufix.Contains(_queryOptions._whatToSearch) ||
									entry.DisplayName.Contains(_queryOptions._whatToSearch))
								searchFound = true;
						}


					}
					subCursor.Close();

					if (!searchFound && _queryOptions._whereToSearch == ContactQuerySearchFields.Name)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				//// https://developer.android.com/reference/android/provider/ContactsContract.CommonDataKinds.StructuredPostal
				//// 	FORMATTED_ADDRESS, TYPE, LABEL, STREET, POBOX, NEIGHBORHOOD, CITY, REGION, POSTCODE, COUNTRY

				entry.Addresses.Clear();
				if (desiredFields.HasFlag(ContactQueryDesiredFields.PostalAddress))
				{
					Android.Database.ICursor subCursor = _contentResolver.Query(
									Android.Provider.ContactsContract.Data.ContentUri,
									new string[] { "data2", "data4", "data7", "data8", "data9", "data10" }, //null,// all columns
																											// ContactsContract.Data.RAW_CONTACT_ID + " = ? AND " + ContactsContract.Data.MIMETYPE + " = ?",
									"raw_contact_id = ? AND mimetype = ?",
									new string[] { contactId.ToString(), Android.Provider.ContactsContract.CommonDataKinds.StructuredPostal.ContentItemType },
									null);   // default order

					//// columnD1 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data1); // FORMATTED_ADDRESS
					//columnD2 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data2); // TYPE
					////columnD3 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data3); // LABEL
					//columnD4 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data4); // STREET
					////columnD5 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data5); // POBOX
					////columnD6 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data6); // NEIGHBORHOOD
					//int columnD7 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data7); // CITY
					//int columnD8 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data8); // REGION (state, province..)
					//int columnD9 = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data9); // POSTCODE
					//int columnDA = subCursor.GetColumnIndex(Android.Provider.ContactsContract.DataColumns.Data10); // COUNTRY
					// columns usage based on: https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.contacts.contact

					for (int itemGuard = 10; itemGuard > 0 && subCursor.MoveToNext(); itemGuard--)
					{
						var itemEntry = new ContactAddress();
						itemEntry.StreetAddress = subCursor.GetString(1);    // we defined columns while opening cursor, so we know what data is in which columns
						itemEntry.Region = subCursor.GetString(3);
						itemEntry.PostalCode = subCursor.GetString(4);
						itemEntry.Locality = subCursor.GetString(2);
						//itemEntry.Description = subCursor.GetString(columnD4);
						itemEntry.Country = subCursor.GetString(5);

						if (!searchFound && _queryOptions._whereToSearch == ContactQuerySearchFields.All)
						{
							if (itemEntry.StreetAddress.Contains(_queryOptions._whatToSearch) ||
									itemEntry.Region.Contains(_queryOptions._whatToSearch) ||
									itemEntry.PostalCode.Contains(_queryOptions._whatToSearch) ||
									itemEntry.Locality.Contains(_queryOptions._whatToSearch) ||
									itemEntry.Country.Contains(_queryOptions._whatToSearch))
								searchFound = true;
						}



						switch (subCursor.GetInt(0))    // we defined columns while opening cursor, so we know what data is in which columns
						{
							case 1: // TYPE_HOME
								itemEntry.Kind = ContactAddressKind.Home;
								break;
							case 2:
								itemEntry.Kind = ContactAddressKind.Work;
								break;
							default:    // TYPE_OTHER
								itemEntry.Kind = ContactAddressKind.Other;
								break;
						}

						entry.Addresses.Add(itemEntry);
					}
					subCursor.Close();

					if (!searchFound && _queryOptions._whereToSearch != ContactQuerySearchFields.None)
					{
						pageGuard++;    // as this item is not returned...
						continue;
					}
				}

				entriesList.Add(entry);
			}

			return entriesList;
		}

	}

	public partial class ContactBatch
	{

		public IReadOnlyList<Contact> Contacts;

		internal ContactBatch(IReadOnlyList<Contact> contacts )
		{
			Contacts = contacts;
		}

	}

	public partial class Contact
	{
		// public string Name { get; set } // can be unimplemented, see firstname (and lastname?)
		// https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.contacts.contact.name

		public IList<ContactEmail> Emails { get; internal set; }
		// Address (321 char), Description (512 char), Kind
		// KIND: Personal, Work, Other

		public IList<ContactAddress> Addresses { get; internal set; }
		// Country (1024), Kind, PostalCode (zip, 1024), Region (state, 1024), StreetAddress (street, 1024), Description (512), Locality (city, 1024)
		// KIND: Home, Work, Other

		public IList<ContactPhone> Phones { get; internal set; }
		// Description (512), Kind, Number (50)
		// KIND: 10 roznych, Home, Mobile, Work, Other, Pager, BusinessFax, HomeFax, Company, Assistant, Radio

		// public string FullName { get; internal set; }

		public string MiddleName { get; set; } // 64

		public string LastName { get; set; } // 64

		public string FirstName { get; set; } // 64 char

		public string HonorificNamePrefix { get; set; } // 32

		public string HonorificNameSufix { get; set; } // 32

		public string DisplayName { get; internal set; }

		public Contact()
		{
			Emails = new List<ContactEmail>();
			Phones = new List<ContactPhone>();
			Addresses = new List<ContactAddress>();
		}

	}

	public enum ContactAddressKind
	{
		Home,
		Work,
		Other,
	}

	public partial class ContactAddress
	{
		public string StreetAddress { get; set; }
		public string Region { get; set; }
		public string PostalCode { get; set; }
		public string Locality { get; set; }
		public ContactAddressKind Kind { get; set; }
		// public string Description { get; set; }
		public string Country { get; set; }
		public ContactAddress()
		{
			// overriding 'missing method'
		}
	}

	public enum ContactPhoneKind
		{
			Home,
			Mobile,
			Work,
			Other,
			Pager,
			BusinessFax,
			HomeFax,
			Company,
			Assistant,
			Radio,
		}

	public partial class ContactPhone
	{
		public string Number { get; set; }
		public ContactPhoneKind Kind { get; set; }
		public ContactPhone()
		{
			// overriding 'missing method'
		}
	}


	public enum ContactEmailKind
	{
		Personal,
		Work,
		Other,
	}

	public partial class ContactEmail
	{
		public ContactEmailKind Kind { get; set;}

		public string Address { get; set; } // 321 chars

		// public string Description { get; set; } // 512 chars
		public ContactEmail()
		{
			// overriding error - method lost
		}
	}


	#endregion

	#region "calllog"

	public partial class PhoneCallHistoryManager
	{

		private static async Task<PhoneCallHistoryStore> RequestStoreAsyncTask(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType accessType)
		{
			// UWP: AppEntriesReadWrite, AllEntriesLimitedReadWrite, AllEntriesReadWrite
			// Android: Manifest has READ_CALL_LOG and WRITE_CALL_LOG, no difference between app/limited/full
			// using: AllEntriesReadWrite as ReadWrite, and AllEntriesLimitedReadWrite as ReadOnly


			var _histStore = new PhoneCallHistoryStore();

			// below API 16 (JellyBean), permission are granted
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean)
			{
				return _histStore;
			}

			// since API 29, we should do something more:
			// https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams.html#setWhitelistedRestrictedPermissions(java.util.Set%3Cjava.lang.String%3E)

			// do we have declared this permission in Manifest?
			// it could be also Coarse, without GPS
			Android.Content.Context context = Android.App.Application.Context;
			Android.Content.PM.PackageInfo packageInfo =
				context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
			var requestedPermissions = packageInfo?.RequestedPermissions;
			if (requestedPermissions is null)
				return null;

			bool bInManifest = false;
			foreach (string oPerm in requestedPermissions)
			{
				if (oPerm.Equals(Android.Manifest.Permission.ReadCallLog, StringComparison.OrdinalIgnoreCase))
					bInManifest = true;
			}

			if (!bInManifest)
				return null;

			// required for contact name
			bInManifest = false;
			foreach (string oPerm in requestedPermissions)
			{
				if (oPerm.Equals(Android.Manifest.Permission.ReadContacts, StringComparison.OrdinalIgnoreCase))
					bInManifest = true;
			}

			if (!bInManifest)
				return null;



			if (accessType == Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesReadWrite)
			{
				bInManifest = false;
				foreach (string oPerm in requestedPermissions)
				{
					if (oPerm.Equals(Android.Manifest.Permission.WriteCallLog, StringComparison.OrdinalIgnoreCase))
						bInManifest = true;
				}

				if (!bInManifest)
					return null;
			}

			List<string> requestPermission = new List<string>();

			// check if permission is granted
			if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.ReadCallLog)
					!= Android.Content.PM.Permission.Granted)
			{
				requestPermission.Add(Android.Manifest.Permission.ReadCallLog);
			}

			if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.ReadContacts)
					!= Android.Content.PM.Permission.Granted)
			{
				requestPermission.Add(Android.Manifest.Permission.ReadContacts);
			}

			if (accessType == Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesReadWrite)
			{
				if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Uno.UI.ContextHelper.Current, Android.Manifest.Permission.WriteCallLog)
						!= Android.Content.PM.Permission.Granted)
				{
					requestPermission.Add(Android.Manifest.Permission.WriteCallLog);
				}
			}

			if (requestPermission.Count < 1)
				return _histStore;

			// system dialog asking for permission

			// this code would not compile here - but it compile in your own app.
			// to be compiled inside Uno, it has to be splitted into layers
			var tcs = new TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

			void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
			{

				if (e.RequestCode == 1)
				{
					tcs.TrySetResult(e);
				}
			}

			var current = Uno.UI.BaseActivity.Current;

			try
			{
				current.RequestPermissionsResultWithResults += handler;

				Android.Support.V4.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, requestPermission.ToArray(), 1);

				var result = await tcs.Task;
				if (result.GrantResults.Length < 1)
					return null;
				if (result.GrantResults[0] == Android.Content.PM.Permission.Granted)
					return _histStore;

			}
			finally
			{
				current.RequestPermissionsResultWithResults -= handler;
			}


			return null;

			//}

			return _histStore;
		}

		public static IAsyncOperation<PhoneCallHistoryStore> RequestStoreAsync(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType accessType)
			=> RequestStoreAsyncTask(accessType).AsAsyncOperation<PhoneCallHistoryStore>();
	}

	public partial class PhoneCallHistoryStore
	{
		public PhoneCallHistoryEntryReader GetEntryReader() => new PhoneCallHistoryEntryReader();

	}

	// https://developer.samsung.com/galaxy/others/calllogs-in-android
	public partial class PhoneCallHistoryEntryReader
	{
		// <uses-permission android:name="android.permission.READ_CONTACTS">  ? A nie calllog?

		private Android.Database.ICursor _cursor = null;

		public PhoneCallHistoryEntryReader()
		{
			Android.Content.ContentResolver cr = Android.App.Application.Context.ContentResolver;

			_cursor = cr.Query(Android.Provider.CallLog.Calls.ContentUri,
									null,
									null,
									null,
									Android.Provider.CallLog.Calls.DefaultSortOrder);   // == date DESC

			string sTmp = MyDUmps.DumpTableHeaderNames(_cursor);
		}

		~PhoneCallHistoryEntryReader()
		{
			if (_cursor != null)
			{
				_cursor.Close();
			}
		}

		public async Task<IReadOnlyList<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry>> ReadBatchAsync()
		{

			var entriesList = new List<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry>();

			if (_cursor is null)
			{
				return entriesList;
			}

			for (int pageGuard = 100; pageGuard > 0 && _cursor.MoveToNext(); pageGuard--)
			{
				var entry = new Windows.ApplicationModel.Calls.PhoneCallHistoryEntry();

				int callType = _cursor.GetInt(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Type));
				switch (callType)
				{
					case 1:
						entry.IsIncoming = true;
						break;
					case 3:
						entry.IsMissed = true;
						break;
					case 4:
						entry.IsVoicemail = true;
						break;
				}

				// https://developer.android.com/reference/android/provider/CallLog.Calls - seconds
				// https://developer.samsung.com/galaxy/others/calllogs-in-android - miliseconds
				entry.Duration = TimeSpan.FromSeconds(_cursor.GetLong(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Duration)));

				entry.StartTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0,
						TimeSpan.FromMilliseconds(_cursor.GetLong(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Date))));

				entry.Address = new Windows.ApplicationModel.Calls.PhoneCallHistoryEntryAddress(
					_cursor.GetString(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.Number)),
					Windows.ApplicationModel.Calls.PhoneCallHistoryEntryRawAddressKind.PhoneNumber);

				try
				{
					entry.Address.DisplayName = _cursor.GetString(_cursor.GetColumnIndex(Android.Provider.CallLog.Calls.CachedName));
				}
				catch
				{
					// can be null
				}

				entriesList.Add(entry);
			}

			return entriesList;
		}
	}


    #endregion

    #region "SMS"

    #region "InsideUno945"
    public partial interface IChatItem
	{
		//Windows.ApplicationModel.Chat.ChatItemKind ItemKind { get; }
	}

	public enum ChatMessageKind	// ale ze to niby moze byc juz zdefiniowane
	{
		Standard,
		FileTransferRequest,
		TransportCustom,
		JoinedConversation,
		LeftConversation,
		OtherParticipantJoinedConversation,
		OtherParticipantLeftConversation,
	}

	public enum ChatMessageOperatorKind // ale ze to niby moze byc juz zdefiniowane
	{
		Unspecified,
		Sms,
		Mms,
		Rcs
	}

	public partial class ChatMessage : IChatItem
	{
		public Windows.ApplicationModel.Chat.ChatItemKind ItemKind { get; }

		public bool IsIncoming { get; set; }
		// public bool IsForwardingDisabled
		//public string TransportId
		public Windows.ApplicationModel.Chat.ChatMessageStatus Status { get; set; }
		public string From { get; set; }
		// public string Subject
		public bool IsRead { get; set; }
		public DateTimeOffset NetworkTimestamp { get; set; }
		public DateTimeOffset LocalTimestamp { get; set; }
		// public global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.ApplicationModel.Chat.ChatMessageStatus> RecipientSendStatuses
		// public string TransportFriendlyName
		// public global::System.Collections.Generic.IList<global::Windows.ApplicationModel.Chat.ChatMessageAttachment> Attachments
		// public string Id
		public bool IsSeen { get; set; }
		public ChatMessageKind MessageKind { get; set; }
		// public bool IsReceivedDuringQuietHours
		//public bool IsAutoReply
		// public ulong EstimatedDownloadSize
		// public global::Windows.ApplicationModel.Chat.ChatConversationThreadingInfo ThreadingInfo
		// public bool ShouldSuppressNotification
		// public string RemoteId
		public ChatMessageOperatorKind MessageOperatorKind { get; set; }
		// public bool IsReplyDisabled
		// public bool IsSimMessage
		// public global::System.Collections.Generic.IList<global::Windows.ApplicationModel.Chat.ChatRecipientDeliveryInfo> RecipientsDeliveryInfos
		// public string SyncId

	}

	#endregion

	public partial class ChatMessageReader
	{
		// https://stackoverflow.com/questions/848728/how-can-i-read-sms-messages-from-the-device-programmatically-in-android

		private TimeSpan _zakres;
		private Android.Database.ICursor _cursor = null;
		private int _colBody, _colTime, _colInOut, _colRead, _colFrom, _colTo, _colSeen, _colAddr, _colStatus;

		internal ChatMessageReader(TimeSpan zakres)
		{
			_zakres = zakres;
			Android.Content.ContentResolver cr = Android.App.Application.Context.ContentResolver;
			//Android.Net.Uri messageUri = Android.Net.Uri.Parse("content://sms/");

			_cursor = cr.Query(Android.Provider.Telephony.Sms.ContentUri , null, null, null, Android.Provider.Telephony.Sms.DefaultSortOrder );

			string sTmp = MyDUmps.DumpTableHeaderNames(_cursor);
			System.Diagnostics.Debug.Write(sTmp);
			string sTmp1 = MyDUmps.DumpTableRows(_cursor);
			System.Diagnostics.Debug.Write(sTmp1);
			if (_cursor.MoveToFirst())
			{ // runtime optimizations
				_colBody = _cursor.GetColumnIndex(Android.Provider.Telephony.TextBasedSmsColumns.Body);
				_colTime = _cursor.GetColumnIndex(Android.Provider.Telephony.TextBasedSmsColumns.Date);	//int
				_colInOut = _cursor.GetColumnIndex(Android.Provider.Telephony.TextBasedSmsColumns.Type ); // 1:inbox, 4:outbox, 2:sent, ...
				_colRead = _cursor.GetColumnIndex(Android.Provider.Telephony.TextBasedSmsColumns.Read );	// int (bool)
				_colAddr = _cursor.GetColumnIndex(Android.Provider.Telephony.TextBasedSmsColumns.Address);
				_colSeen = _cursor.GetColumnIndex(Android.Provider.Telephony.TextBasedSmsColumns.Seen); // int (bool)
				_colStatus = _cursor.GetColumnIndex(Android.Provider.Telephony.TextBasedSmsColumns.Status); // int (bool)
					//0: _id
					//1: thread_id
					//2: address
					//3: person
					//4: date
					//5: protocol
					//6: read
					//7: status
					//8: type
					//9: reply_path_present
					//10: subject
					//11: body
					//12: service_center
					//13: locked
			}
		}
		~ChatMessageReader()
		{
			if (_cursor != null)
			{
				_cursor.Close();
			}
		}

		// inna wersja ma jako paramert count of SMS
		public IAsyncOperation<IReadOnlyList<Windows.ApplicationModel.Chat.ChatMessage>> ReadBatchAsync()
			=> ReadBatchAsyncTask().AsAsyncOperation< IReadOnlyList < Windows.ApplicationModel.Chat.ChatMessage >> ();

		public async Task<IReadOnlyList<Windows.ApplicationModel.Chat.ChatMessage>> ReadBatchAsyncTask()
		{
			var entriesList = new List<Windows.ApplicationModel.Chat.ChatMessage>();

			if (_cursor is null || _cursor.IsAfterLast)
			{
				return entriesList;
			}

			for (int pageGuard = 100; pageGuard > 0 ; pageGuard--)
			{
				var entry = new Windows.ApplicationModel.Chat.ChatMessage();

				entry.MessageKind = Windows.ApplicationModel.Chat.ChatMessageKind.Standard;
				entry.MessageOperatorKind = Windows.ApplicationModel.Chat.ChatMessageOperatorKind.Sms;

				entry.Body = _cursor.GetString(_colBody);
				entry.IsIncoming = _cursor.GetInt(_colInOut) == 1;
				entry.LocalTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(_cursor.GetLong(_colTime));
				entry.NetworkTimestamp = entry.LocalTimestamp;
				entry.Recipients.Add(_cursor.GetString(_colAddr));

				if (entry.IsIncoming)
				{
					entry.IsRead = _cursor.GetInt(_colRead) != 0;
					entry.IsSeen = _cursor.GetInt(_colSeen) != 0;
					entry.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.Received;
				}
				else
				{
					entry.IsRead = true;
					entry.IsSeen = true;
					switch (_cursor.GetInt(_colStatus))
					{
						case (int)Android.Provider.SmsStatus.Complete:
						case (int)Android.Provider.SmsStatus.None:
							entry.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.Sent;
							break;
						case (int)Android.Provider.SmsStatus.Failed:
							entry.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.SendFailed;
							break;
						case (int)Android.Provider.SmsStatus.Pending:
							entry.Status = Windows.ApplicationModel.Chat.ChatMessageStatus.Sending;
							break;
					}
				}

				entriesList.Add(entry);

				if (!_cursor.MoveToNext())
					break;
			}

			return entriesList;
		}
	}


	public partial class ChatMessageManager
	{
		public static IAsyncOperation<ChatMessageStore> RequestStoreAsync()
			=> RequestStoreAsyncTask().AsAsyncOperation<ChatMessageStore>();

		public static async Task<ChatMessageStore> RequestStoreAsyncTask()
		{

			string[] required = { Android.Manifest.Permission.ReadSms };
			string[] optional = { Android.Manifest.Permission.WriteSms };
			string[] missing = AndroidHelpers.MissingPermissions(required, optional);

			if (!await AndroidHelpers.AndroidPermissionAsync(
				new string[] { Android.Manifest.Permission.ReadSms },
				new string[] { Android.Manifest.Permission.WriteSms }))
			{
				return null;
			}

			return new ChatMessageStore();
		}

		public static async Task<ChatMessageStore> RequestStoreAsyncTaskOld()
		{

			string[] required = { Android.Manifest.Permission.ReadSms };
			string[] optional = { Android.Manifest.Permission.WriteSms };
			string[] missing = AndroidHelpers.MissingPermissions(required, optional);
			var cos = AndroidHelpers.MissingPermissions(new string[] { Android.Manifest.Permission.ReadSms }, optional);
			if (missing is null)
			{ // some error occured
				return null;
			}

			if (missing.Count() < 1)
			{ // permissions granted
				return new ChatMessageStore();
			}

			var tcs = new TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

			void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
			{

				if (e.RequestCode == 1)
				{
					tcs.TrySetResult(e);
				}
			}

			var current = Uno.UI.BaseActivity.Current;

			try
			{
				current.RequestPermissionsResultWithResults += handler;

				Android.Support.V4.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, missing, 1);

				var result = await tcs.Task;
				if (result.GrantResults.Length < 1)
					return null;
				if (result.GrantResults[0] == Android.Content.PM.Permission.Granted)
					return new ChatMessageStore();

			}
			finally
			{
				current.RequestPermissionsResultWithResults -= handler;
			}

			return null;

		}
	}

		public partial class ChatMessageStore
		{
		public ChatMessageReader GetMessageReader() => new ChatMessageReader(new TimeSpan(36500, 0, 0, 0));
		public ChatMessageReader GetMessageReader(TimeSpan recentTimeLimit) => new ChatMessageReader(recentTimeLimit);
		public IAsyncAction SaveMessageAsync(Windows.ApplicationModel.Chat.ChatMessage chatMessage)
			=> SaveMessageAsyncTask(chatMessage).AsAsyncAction();

		internal async Task SaveMessageAsyncTask(Windows.ApplicationModel.Chat.ChatMessage chatMessage)
		{
			// 1. maybe test permission (for write)?
			var currSmsApp = Android.Provider.Telephony.Sms.GetDefaultSmsPackage(Android.App.Application.Context);
			if(currSmsApp != Android.App.Application.Context.PackageName)
			{
				// throw new UnauthorizedAccessException("ChatMessageStore: only app selected as default SMS app can write do SMS store");
			}

			// 2. new SMS
			Android.Content.ContentValues newSMS = new Android.Content.ContentValues();

			newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Body, chatMessage.Body);

			if (chatMessage.IsIncoming)
			{
				newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Type, 1);
				newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Read, chatMessage.IsRead?1:0);
				newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Seen, chatMessage.IsSeen?1:0);
				newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Status, (int)Android.Provider.SmsStatus.None);
				newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Address, chatMessage.From);

			}
			else
			{
				newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Type, 4);
				newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Read, 1);
				newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Seen, 1);

				if (chatMessage.Recipients.Count > 0)
					newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Address, chatMessage.Recipients.ElementAt(0));

				switch (chatMessage.Status)
				{
					case Windows.ApplicationModel.Chat.ChatMessageStatus.ReceiveDownloadFailed:
					case Windows.ApplicationModel.Chat.ChatMessageStatus.SendFailed:
						newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Status, (int)Android.Provider.SmsStatus.Failed);
						break;
					default:
						newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Status, (int)Android.Provider.SmsStatus.Pending);
						break;
				}
			}

			// z Android do Windows działa poprawnie:
			// entry.LocalTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(_cursor.GetLong(_colTime));
			long msecs = chatMessage.LocalTimestamp.ToUnixTimeMilliseconds();
			// moje wlasne
			//TimeSpan dto = chatMessage.LocalTimestamp - new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan(0));
			//long msec1 = (long)dto.TotalMilliseconds;
			newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.Date, msecs);
			msecs = chatMessage.NetworkTimestamp.ToUnixTimeMilliseconds();
			newSMS.Put(Android.Provider.Telephony.TextBasedSmsColumns.DateSent, msecs);

			// 3. insert into Uri
			Android.Content.ContentResolver cr = Android.App.Application.Context.ContentResolver;
			var retVal = cr.Insert(Android.Provider.Telephony.Sms.ContentUri, newSMS);
			if (retVal is null)
			{
				// some error, maybe throw Exception?
			}
		}
	}


	//  Windows.ApplicationModel.Chat.ChatMessageReader oRdr = null;
	// Windows.ApplicationModel.Chat.ChatMessageManager.RequestStoreAsync();
	// oRdr = oStore.GetMessageReader();
	// oRdr.ReadBatchAsync();
	// oStore.SaveMessageAsync(msg);


	/*
	Windows.ApplicationModel.Chat.ChatMessageManager.RequestStoreAsync()
	oStore.GetMessageReader()

	oRdr.ReadBatchAsync()

	oMsg.IsIncoming
	oMsg.From
	oMsg.LocalTimestamp

	dodac ewentualnie zamiane vbCrLf na "\n" przed zapisaniem


	no i nie ma tez pliku:
	Windows.Storage.KnownFolders.RemovableDevices
	externalDevices.GetFoldersAsync() [lista kart SD]

	do importu:
	Windows.Storage.Pickers.FileOpenPicker()
	picker.FileTypeFilter
	picker.PickSingleFileAsync()

	Windows.Storage.FileIO.ReadTextAsync

	obu stron:
	oMsg.MessageKind = Windows.ApplicationModel.Chat.ChatMessageKind.Standard;
	oMsg.MessageOperatorKind = Windows.ApplicationModel.Chat.ChatMessageOperatorKind.Sms;

	oMsg.IsIncoming
	oMsg.From
	oMsg.IsRead
	oMsg.IsSeen
	oMsg.Status
	oMsg.LocalTimestamp
	oMsg.NetworkTimestamp

	oStore.SaveMessageAsync */

	#endregion

	#region "FilePicker"
	public partial class FileOpenPicker
	{
		private List<string> _fileTypeFilter = new List<string>();
		private string _docUri = "";

		public PickerLocationId SuggestedStartLocation { get; set; } = PickerLocationId.DocumentsLibrary;
		public string SettingsIdentifier { get; set; } = "";
		public IList<string> FileTypeFilter => _fileTypeFilter;

		private List<string> UWPextension2Mime()
		{
			var mimeTypes = new List<string>();
			if (_fileTypeFilter.Count < 1)
			{
				mimeTypes.Add("*/*");
				return mimeTypes;
			}

			foreach (var extension in _fileTypeFilter)
			{
				// extension is in form of ".png", but GetMimeTypeFromExtension requires "png"
				string mimetype = Android.Webkit.MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension.Substring(1));
				bool alreadyExist = false;
				foreach (var mime in mimeTypes)
				{
					if (mime == mimetype)
					{
						alreadyExist = true;
					}
				}
				if (!alreadyExist)
				{
					mimeTypes.Add(mimetype);
				}
			}
			return mimeTypes;
		}

		private void SetDocUri()
		{
			string settName = "_UnoFilePickerSettings" + SettingsIdentifier;

			if (Windows.Storage.ApplicationData.Current.RoamingSettings.Values.ContainsKey(settName))
			{
				// string lastFolder = Windows.Storage.ApplicationData.Current.RoamingSettings.Values[settName].ToString();
				_docUri = Windows.Storage.ApplicationData.Current.RoamingSettings.Values[settName].ToString();
					//Uri.UnescapeDataString(lastFolder);
				return;
			}

			switch(SuggestedStartLocation)
			{
				// see also Windows.Storage.KnownFolders
				case PickerLocationId.DocumentsLibrary:
					_docUri = "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).CanonicalPath;
					break;
				case PickerLocationId.MusicLibrary:
					_docUri = "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).CanonicalPath;
					break;
				case PickerLocationId.PicturesLibrary:
					// Warning: Camera probably is outside this folder!
					_docUri = "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).CanonicalPath;
					break;
				case PickerLocationId.Downloads:
					_docUri = "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).CanonicalPath;
					break;
				case PickerLocationId.VideosLibrary:
					_docUri = "file://" + Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).CanonicalPath;
					break;
				case PickerLocationId.Unspecified:
					_docUri = "";
					break;
				default:
					throw new NotImplementedException("FileOpenPicker unimplemented type of initial dir");

			}

		}

		public FileOpenPicker()
		{
			if(Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Kitkat)
			{
				throw new NotImplementedException("FileOpenPicker requires Android KitKat (API level 19) or newer");
			}
			SetDocUri();
			// check permission: READ_EXTERNAL_STORAGE
		}

		public IAsyncOperation<Windows.Storage.StorageFile> PickSingleFileAsync()
			=> PickSingleFileAsyncTask().AsAsyncOperation<Windows.Storage.StorageFile>();

		public IAsyncOperation<IReadOnlyList<Windows.Storage.StorageFile>> PickMultipleFilesAsync()
			=> PickMultipleFilesAsyncTask().AsAsyncOperation();

		public async Task<Windows.Storage.StorageFile> PickSingleFileAsyncTask()
		{
			var pickedFiles = await PickFilesAsync(false);
			if (pickedFiles is null)
			{
				return null;
			}

			return pickedFiles.ElementAt(0);
		}
		public async Task<IReadOnlyList<Windows.Storage.StorageFile>> PickMultipleFilesAsyncTask()
		{
			var pickedFiles = await PickFilesAsync(true);
			if (pickedFiles is null)
			{
				return null;
			}

			return pickedFiles.AsReadOnly();
		}

		private TaskCompletionSource<List<Windows.Storage.StorageFile>> completionSource;

		private string PathFromUri(Android.Net.Uri fileUri)
		{
			// basic file
			if (fileUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
			{
				return fileUri.Path;
			}

			// more complicated Uris ("content://")
			if (Android.Provider.DocumentsContract.IsDocumentUri(Android.App.Application.Context, fileUri))
			{
				if (fileUri.Authority.Equals("com.microsoft.skydrive.content.StorageAccessProvider"))
				{
					throw new NotImplementedException("FileOpenPicker - Onedrive documents are not implemented yet");
				}

				string columnName = Android.Provider.MediaStore.Files.FileColumns.Data;
				string[] projection = { columnName };
				var cursor = Android.App.Application.Context.ContentResolver.Query(fileUri, projection, null, null, null);
				if (cursor is null)
					throw new Exception("FileOpenPicker - cannot get cursor");

				if (!cursor.MoveToFirst())
					throw new Exception("FileOpenPicker - cannot MoveToFirst");

				int columnNo = cursor.GetColumnIndex(columnName);
				if (columnNo < 0)
					throw new Exception("FileOpenPicker - column with data doesn't exist?");

				string filePath = cursor.GetString(columnNo);
				cursor.Close();
				cursor.Dispose();

				if (!string.IsNullOrEmpty(filePath))
				{
					return filePath;
				}

			}
			throw new NotImplementedException("FileOpenPicker - not implemented document type");

		}

		private Task<List<Windows.Storage.StorageFile>> PickFilesAsync(bool multiple)
		{
			var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(FileOpenPickerActivity));
			// put parameters into Intent
			intent.PutExtra("multiple", multiple);
			intent.PutExtra("initialdir", _docUri);
			intent.PutExtra("mimetypes", UWPextension2Mime().ToArray());

			// wrap it in Task
			completionSource = new TaskCompletionSource<List<Windows.Storage.StorageFile>>();
			FileOpenPickerActivity.FilePicked += FilePickerHandler;

			Android.App.Application.Context.StartActivity(intent);

			async void FilePickerHandler(object sender, List<Android.Net.Uri> list)
			{
				FileOpenPickerActivity.FilePicked -= FilePickerHandler;

				// convert list of Uris tolist of StorageFiles
				var storageFiles = new List<Windows.Storage.StorageFile>();

				if (list.Count > 0)
				{
					string settName = "_UnoFilePickerSettings" + SettingsIdentifier;
					Windows.Storage.ApplicationData.Current.RoamingSettings.Values[settName] = list.ElementAt(0).ToString();
				}

				foreach (var fileUri in list)
				{
					Android.App.Application.Context.ContentResolver.TakePersistableUriPermission(
						fileUri, Android.Content.ActivityFlags.GrantReadUriPermission);

					string filePath = PathFromUri(fileUri);
					if(!string.IsNullOrEmpty(filePath))
					{
						var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
						storageFiles.Add(storageFile);
					}

				}
				completionSource.SetResult(storageFiles);
			}

			return completionSource.Task;
		}

	}


	[Android.App.Activity(ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	internal class FileOpenPickerActivity : Android.App.Activity
	{
		internal static event EventHandler<List<Android.Net.Uri>> FilePicked;
		private bool _multiple;

		private Android.Content.Intent CreateIntent(bool multiple, List<string> mimeTypes, string initialDir)
		{
			var intent = new Android.Content.Intent(Android.Content.Intent.ActionOpenDocument);

			// file ext / mimetypes
			if (mimeTypes.Count < 1)
			{
				intent.SetType("*/*");  // nie mamy nic - to mamy :)
			}
			else
			{
				if (mimeTypes.Count < 2)
				{
					intent.SetType(mimeTypes.ElementAt(0));
				}
				else
				{
					intent.SetType("*/*");
					intent.PutExtra(Android.Content.Intent.ExtraMimeTypes, mimeTypes.ToArray());
				}
			}

			// multiple selection
			if (multiple)
			{
				intent.PutExtra(Android.Content.Intent.ExtraAllowMultiple, true);
			}

			// initial dir
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O && !string.IsNullOrEmpty(initialDir))
			{
				intent.PutExtra(Android.Provider.DocumentsContract.ExtraInitialUri, initialDir);
			}

			// chyba takze
			// intent.AddCategory(Android.Content.Intent.CategoryOpenable);
			// otwieralne w sensie: ContentResolver#openFileDescriptor(Uri, String) // string: rwt (t:can truncate)

			return intent;
		}
		protected override void OnCreate(Android.OS.Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			var caller = base.Intent.Extras;
			_multiple = caller.GetBoolean("multiple", false);

			// startpicker
			var intent = CreateIntent(_multiple,
				caller.GetStringArray("mimetypes").ToList(),
				caller.GetString("initialdir", "")
				);
			// StartActivityForResult(Android.Content.Intent.CreateChooser(intent, "Select file"),10);
			StartActivityForResult(intent, 10);
		}

		protected override void OnActivityResult(int requestCode, Android.App.Result resultCode, Android.Content.Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			var pickedFiles = new List<Android.Net.Uri>();

			if (resultCode != Android.App.Result.Canceled)
			{
				if (_multiple)
				{
					// multiple files - ClipData
					if (data?.ClipData != null)
					{
						for (int i = 0; i < data.ClipData.ItemCount; i++)
						{
							var item = data.ClipData.GetItemAt(i);
							pickedFiles.Add(item.Uri);
						}
					}
				}
				else
				{
					// single file - Data
					if (data?.Data != null)
					{
						pickedFiles.Add(data.Data);
					}

				}
			}

			FilePicked?.Invoke(null, pickedFiles);
		}
	}

	public enum PickerLocationId
	{
		DocumentsLibrary,
		ComputerFolder,
		Desktop,
		Downloads,
		HomeGroup,
		MusicLibrary,
		PicturesLibrary,
		VideosLibrary,
		Objects3D,
		Unspecified,
	}


	#endregion

	#region "Permissions"

	internal class AndroidHelpers
	{
        #region "Permissions"
        private static TaskCompletionSource<bool> _permissionCompletionSource;

		/// <summary>
		/// Return null on error, or array of permission to be asked for (not granted at this time). Both parameters can be null
		/// </summary>
		public static string[] MissingPermissions(string[] requiredPermissions, string[] optionalPermissions)
		{
			// since API 29, we should do something more:
			// https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams.html#setWhitelistedRestrictedPermissions(java.util.Set%3Cjava.lang.String%3E)

			// do we have declared this permission in Manifest?
			Android.Content.Context context = Android.App.Application.Context;
			Android.Content.PM.PackageInfo packageInfo =
				context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
			var manifestPermissions = packageInfo?.RequestedPermissions;
			if (manifestPermissions is null)
				return null;

			// test required permissions
			if (requiredPermissions != null)
			{
				foreach (string permission in requiredPermissions)
				{
					bool foundInManifest = false;
					foreach (string oPerm in manifestPermissions)
					{
						if (oPerm.Equals(permission, StringComparison.OrdinalIgnoreCase))
							foundInManifest = true;

					}
					if (!foundInManifest) return null;
				}
			}

			// prepare list of all permissions
			var allPermissions = new List<string>();
			if (requiredPermissions != null)
				allPermissions.AddRange(requiredPermissions.ToList());

			// add all optional permission, found in Manifest
			if (optionalPermissions != null)
			{
				foreach (string permission in optionalPermissions)
				{
					foreach (string oPerm in manifestPermissions)
					{
						if (oPerm.Equals(permission, StringComparison.OrdinalIgnoreCase))
							allPermissions.Add(permission);
					}
				}
			}

			// prepare list of permission to ask for
			List<string> askForPermission = new List<string>();

			// check if permission is granted
			foreach (var permission in allPermissions)
			{
				if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(context, permission)
						!= Android.Content.PM.Permission.Granted)
				{
					askForPermission.Add(permission);
				}
			}

			return askForPermission.ToArray();

		}

		/// <summary>
		/// Return true if granted are all requiredPermissions, and all optionalPermissions mentioned in Manifest. Both parameters can be null
		/// Warning: not working! Error in Mono?
		/// </summary>
		public static Task<bool> AndroidPermissionAsync(string[] requiredPermissions, string[] optionalPermissions)
		{
			// prepare return value
			_permissionCompletionSource = new TaskCompletionSource<bool>();

			var askForPermission = MissingPermissions(requiredPermissions, optionalPermissions);
			if(askForPermission is null)
			{
				_permissionCompletionSource.SetResult(false); // signal: "permission denied", although there is some error
				return _permissionCompletionSource.Task;
			}


			if (askForPermission.Count() < 1)
			{
				_permissionCompletionSource.SetResult(true); // signal: "permission granted"
				return _permissionCompletionSource.Task;
			}

			// system dialog asking for permission

			var intermediaryIntent = new Android.Content.Intent(Android.App.Application.Context, typeof(AskForPermission));
			// put parameters into Intent
			intermediaryIntent.PutExtra("permissions", askForPermission.ToArray());

			// wrap it in Task
			AskForPermission.AfterDialog += AfterPermissionDialog;

			Android.App.Application.Context.StartActivity(intermediaryIntent);

			void AfterPermissionDialog(object sender, bool granted)
			{
				AskForPermission.AfterDialog -= AfterPermissionDialog;
				_permissionCompletionSource.SetResult(granted);
			}
			return _permissionCompletionSource.Task;
		}
		[Android.App.Activity(ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
		internal class AskForPermission : Android.App.Activity
		{
			internal static event EventHandler<bool> AfterDialog;

			protected override void OnCreate(Android.OS.Bundle savedInstanceState)
			{
				base.OnCreate(savedInstanceState);

				var caller = base.Intent.Extras;
				var permissionsArray = caller?.GetStringArray("permissions");
				if (permissionsArray is null)
				{
					throw new Exception("AskForPermission:OnCreate - empty permission array in Intent.Extras?");
				}

				// Android.Support.V4.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current, permissionsArray, 1);
				RequestPermissions(permissionsArray, 1);

			}

			public override void OnRequestPermissionsResult(int requestCode, String[] permissions, Android.Content.PM.Permission[] grantResults)
			{
				base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

				bool allGranted = true;

				for (int i = 0; i < grantResults.Count(); i++)
				{
					if (grantResults[i] != Android.Content.PM.Permission.Granted)
					{
						allGranted = false;
					}
				}

				AfterDialog?.Invoke(null, allGranted);
				Finish();   // activity.finish
			}

		}
        #endregion 

        #region "InvokeIntentAsync"
        private static TaskCompletionSource<bool> _intentCompletionSource;
		/// <summary>
		/// Run Android Intent, and wait for its end (no return value!)
		/// Warning: not re-entrant!
		/// </summary>
		public static Task InvokeIntentAsync(Android.Content.Intent intent)
		{
			if(intent is null)
			{
				throw new ArgumentNullException("InvokeIntentAsync called with null Intent");
			}

			if(InvokeIntentActivity.intentToInvoke != null)
			{
				throw new Exception("InvokeIntentAsync is not re-entrant! (and called inside other InvokeIntentAsync)");
			}

			_intentCompletionSource = new TaskCompletionSource<bool>();
			InvokeIntentActivity.AfterHandler += InvokeIntentHandler;
			InvokeIntentActivity.intentToInvoke = intent;

			var intermediaryIntent = new Android.Content.Intent(Android.App.Application.Context, typeof(InvokeIntentActivity));
			Android.App.Application.Context.StartActivity(intermediaryIntent);

			void InvokeIntentHandler(object sender, bool dummy)
			{
				InvokeIntentActivity.AfterHandler -= InvokeIntentHandler;
				InvokeIntentActivity.intentToInvoke = null;
				_intentCompletionSource.SetResult(dummy);
			}

			return _intentCompletionSource.Task;
		}

		[Android.App.Activity(ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
		internal class InvokeIntentActivity : Android.App.Activity
		{
			internal static event EventHandler<bool> AfterHandler;
			internal static Android.Content.Intent intentToInvoke = null;

			protected override void OnCreate(Android.OS.Bundle savedInstanceState)
			{
				base.OnCreate(savedInstanceState);
				StartActivityForResult(intentToInvoke, 10);
			}

			protected override void OnActivityResult(int requestCode, Android.App.Result resultCode, Android.Content.Intent data)
			{
				base.OnActivityResult(requestCode, resultCode, data);
				AfterHandler?.Invoke(null,true);
				Finish(); // finish activity
			}

		}
	}

    #endregion 

	

	#endregion
}


