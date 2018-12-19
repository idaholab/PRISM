using System;


[Serializable]
public class LIVESession
{
	public LIVESession ()
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
		return string.Format ("[LIVESession: id={0}, name={1}, dataStreamName={2}, controlStreamName={3}, activemqUrl={4}]", id, name, dataStreamName, controlStreamName, activemqUrl);
	}
}


