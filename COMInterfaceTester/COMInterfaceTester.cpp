// COMInterfaceTester.cpp : 定義主控台應用程式的進入點。
//

#include "stdafx.h"
#include <iostream>
#import "WPSXWrapper.tlb" named_guids raw_interfaces_only

using namespace std;
using namespace WPSXWrapper;



int main()
{
	// Initialize all COM Components
	CoInitialize(NULL);

	// <namespace>::<InterfaceName>
	// a pointer to the dot net COM object
	WPSXWrapper::WPSX_COM_InterfacePtr pTracker;
	
	cout << "CreateInstance...\n";
	HRESULT hResult = pTracker.CreateInstance(WPSXWrapper::CLSID_WPSXTracker_Net);
	if (hResult == S_OK)
	{
		cout << "CreateInstance success\n";

		VARIANT_BOOL bReturn;
		BSTR strServerAddress = SysAllocString(L"http://10.10.12.39");
		BSTR strVersion = SysAllocString(L"v5.2.0");
		BSTR strUserName = SysAllocString(L"Bob");
		BSTR strAppLocale = SysAllocString(L"en-US");
		BSTR strAppName = SysAllocString(L"WPSX");
		hResult = pTracker->Initialize(strServerAddress, strVersion, strUserName, strAppLocale, 2, strAppName, 800, 600, false, &bReturn);

		if (hResult == S_OK && bReturn == VARIANT_TRUE)
		{
			cout << "Initialize success\n";

			// dictionary
			BSTR strDictionary = SysAllocString(L"Basic");			
			BSTR strSourceLang = SysAllocString(L"fr");
			BSTR strDestLang = SysAllocString(L"en");
			BSTR strEngine = SysAllocString(L"Google");
			hResult = pTracker->SendDictionaryRecord(strDictionary, strSourceLang, strDestLang, &bReturn);

			if (hResult == S_OK && bReturn == VARIANT_TRUE)
				cout << "SendDictionaryRecord success\n";
			else
				cout << "SendDictionaryRecord failed!\n";
			system("pause");

			// easy dict
			hResult = pTracker->SendEasyDictRecord(strDictionary, strSourceLang, strDestLang, &bReturn);
			if (hResult == S_OK && bReturn == VARIANT_TRUE)
				cout << "SendEasyDictRecord success\n";
			else
				cout << "SendEasyDictRecord failed!\n";
			system("pause");

			// scan			
			hResult = pTracker->SendScanRecord(strSourceLang, &bReturn);

			if (hResult == S_OK && bReturn == VARIANT_TRUE)
				cout << "SendScanRecord success\n";
			else
				cout << "SendScanRecord failed!\n";
			system("pause");

			// translate			
			hResult = pTracker->SendTranslateRecord(strDictionary, strSourceLang, strDestLang, &bReturn);
			if (hResult == S_OK && bReturn == VARIANT_TRUE)
				cout << "SendTranslateRecord success\n";
			else
				cout << "SendTranslateRecord failed!\n";
		}

		// free the object
		pTracker->Release();
	}
	else
	{
		cout << "CreateInstance failed!\n";
	}
	system("pause");
    return 0;
}

