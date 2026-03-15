using System.Xml;

namespace ACS.Core.Workflow;

public enum eJobResult
{
    SUCCESS,
    UNKNOWN,
    ASYNCJOB,
    TIMEOUT,
    FAIL,
    WAIT,
    ERROR,
}

public interface IWorkflowManager
{
    bool Execute(string paramString, object paramObject);
    bool Execute(string paramString, object paramObject, bool paramBoolean);
    bool Execute(string paramString, XmlDocument paramDocument);
    bool Execute(string paramString, XmlDocument paramDocument, bool paramBoolean);
    bool Execute(string paramString, object[] paramArrayOfObject);
    bool Execute(string paramString, object[] paramArrayOfObject, bool paramBoolean);
    bool Execute(string paramString1, string paramString2, XmlDocument paramDocument);
    bool Execute(string paramString1, string paramString2, XmlDocument paramDocument, bool paramBoolean);
    bool Execute(string paramString1, string paramString2, object paramObject);
    bool Execute(string paramString1, string paramString2, object paramObject, bool paramBoolean);
    bool Execute(string paramString1, string paramString2, object[] paramArrayOfObject, bool paramBoolean);
    void Reload();
    bool SkipWorkflow(string paramString);
    void Start();
    void Stop();
}
