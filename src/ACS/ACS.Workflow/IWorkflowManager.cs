using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Workflow
{
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
        bool Execute(String paramString, Object paramObject);

        bool Execute(String paramString, Object paramObject, bool paramBoolean);

        bool Execute(String paramString, XmlDocument paramDocument);

        bool Execute(String paramString, XmlDocument paramDocument, bool paramBoolean);

        bool Execute(String paramString, Object[] paramArrayOfObject);

        bool Execute(String paramString, Object[] paramArrayOfObject, bool paramBoolean);

        bool Execute(String paramString1, String paramString2, XmlDocument paramDocument);

        bool Execute(String paramString1, String paramString2, XmlDocument paramDocument, bool paramBoolean);

        bool Execute(String paramString1, String paramString2, Object paramObject);

        bool Execute(String paramString1, String paramString2, Object paramObject, bool paramBoolean);

        bool Execute(String paramString1, String paramString2, Object[] paramArrayOfObject, bool paramBoolean);

        void Reload();

        bool SkipWorkflow(String paramString);

        void Start();

        void Stop();
    }
}
