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


