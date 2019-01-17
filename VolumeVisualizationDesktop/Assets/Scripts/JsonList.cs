
/* 
 * Copyright 2019 Idaho National Laboratory.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System;
using System.Collections.Generic;

[Serializable]
public class JsonList<T>
{
	public JsonList ()
	{
	}

	public int total
	{
		get;
		set;
	}

	public T[] data {
		get;
		set;
	}

	public override string ToString ()
	{
		string result = "";
		for (int ii = 0; ii < data.Length; ii++)
			result += string.Format ("{{{0}}}", data [ii]);
		return string.Format ("[JsonList: total={0}, data=[{1}]]", total, result);
	}
}


