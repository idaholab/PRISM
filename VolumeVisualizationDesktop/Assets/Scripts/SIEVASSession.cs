/***
 * 

Copyright 2017 Idaho National Laboratory.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

***/


using System;
using UnityEngine;


[Serializable]
public class SIEVASSession
{
	public SIEVASSession ()
	{
	}

	public long id {
		get;
		set;
	}

	public string name {
		get;
		set;
	}

	public string dataStreamName
	{
		get;
		set;
	}

	public string controlStreamName
	{
		get;
		set;
	}

	public String activemqUrl
	{
		get;
		set;
	}

	

	public override string ToString ()
	{
		return string.Format ("[SIEVASSession: id={0}, name={1}, dataStreamName={2}, controlStreamName={3}, activemqUrl={4}]", id, name, dataStreamName, controlStreamName, activemqUrl);
	}
}


