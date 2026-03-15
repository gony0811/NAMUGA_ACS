using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace ACS.Core.Base.Interface
{
    public interface IPersistentDao
    {
        bool Use(Type class1); //bool use(Class class1);

        bool Use(string s);

        void Save(Object obj);

        bool Save(Object obj, bool flag);

        void SaveOrUpdate(Object obj);

        void Flush();

        void UpdateAll(ICollection collection); //updateAll(Collection collection);

        IList<T> Find<T>(string s) where T : class; // List find(string s);

        Object Find(Type class1, ISerializable serializable);

        Object Find(string s, ISerializable serializable);

        Object Find(Type class1, ISerializable serializable, bool flag);

        Object Find(string s, ISerializable serializable, bool flag);

        Object FindByName(Type class1, Object obj);

        Object FindByName(string s, Object obj);

        Object Exist(Type class1, ISerializable serializable);

        Object Exist(string s, ISerializable serializable);

        Object ExistByName(Type class1, Object obj);

        Object ExistByName(string s, Object obj);

        Object FindByName(Type class1, Object obj, bool flag);

        Object FindByName(string s, Object obj, bool flag);

        IList FindByExample(Object obj);

        IList FindByExample(Object obj, bool flag);

        IList FindByAttribute(Type class1, string s, object obj);

        IList FindByAttribute(string s, string s1, object obj);

        IList FindByAttribute(Type class1, string s, object obj, bool flag);

        IList FindByAttribute(string s, string s1, object obj, bool flag);

        IList FindByAttributeOrderBy(Type class1, string s, object obj, string s1);

        IList FindByAttributeOrderBy(string s, string s1, object obj, string s2);

        IList FindByAttributeOrderBy(Type class1, string s, object obj, string s1, bool flag);

        IList FindByAttributeOrderBy(string s, string s1, object obj, string s2, bool flag);

        IList FindByAttributeOrderByDesc(Type class1, string s, object obj, string s1);

        IList FindByAttributeOrderByDesc(string s, string s1, object obj, string s2);

        IList FindByAttributeOrderByDesc(Type class1, string s, object obj, string s1, bool flag);

        IList FindByAttributeOrderByDesc(string s, string s1, object obj, string s2, bool flag);

        IList FindByAttributes(Type class1, Dictionary<string, object> map);

        IList FindByAttributes(string s, Dictionary<string, object> map);

        IList FindByAttributesOrderBy(Type class1, Dictionary<string, object> map, string s);

        IList FindByAttributesOrderBy(string s, Dictionary<string, object> map, string s1);

        IList FindByAttributesOrderByDesc(Type class1, Dictionary<string, object> map, string s);

        IList FindByAttributesOrderByDesc(string s, Dictionary<string, object> map, string s1);

        IList FindAll(Type class1);

        IList FindAll(string s);

        IList FindAllOrderBy(Type class1, string s);

        IList FindAllOrderBy(string s, string s1);

        IList FindAllOrderByDesc(Type class1, string s);

        IList FindAllOrderByDesc(string s, string s1);

        IList FindProperty(Type class1, string s);

        IList FindProperty(string s, string s1);

        IList FindPropertyOrderBy(Type class1, string s, string s1);

        IList FindPropertyOrderBy(string s, string s1, string s2);

        IList FindPropertyByAttributes(Type class1, string s, string s1, Object obj);

        IList FindPropertyByAttributes(string s, string s1, string s2, Object obj);

        IList FindPropertyByAttributesOrderBy(Type class1, string s, string s1, Object obj, string s2);

        IList FindPropertyByAttributesOrderBy(string s, string s1, string s2, Object obj, string s3);

        IList FindByAttributesOR(Type class1, Dictionary<string, object> map);

        IList FindByAttributesOR(string s, Dictionary<string, object> map);

        IList FindPropertyByAttributesOR(Type class1, string s, Dictionary<string, object> map);

        IList FindPropertyByAttributesOR(string s, string s1, Dictionary<string, object> map);

        int UpdateByHql(string s);

        int UpdateByHql(string s, string s1);

        int UpdateByHql(string s, string[] vs);

        int UpdateByHql(string s, ArrayList list);

        void Update(Object obj);

        void Update(Object obj, bool flag);

        int Update(Type class1, string s, Object obj, string s1);

        int Update(Type class1, Dictionary<string, object> map, string s);

        int Update(string s, string s1, Object obj, string s2);

        int Update(string s, Dictionary<string, object> map, string s1);

        int UpdateByName(Type class1, string s, Object obj, string s1);

        int UpdateByName(Type class1, Dictionary<string, object> map, string s);

        int UpdateByName(string s, string s1, Object obj, string s2);

        int UpdateByName(string s, Dictionary<string, object> map, string s1);

        int UpdateByAttribute(Type class1, string s, Object obj, string s1, Object obj1);

        int UpdateByAttribute(string s, string s1, Object obj, string s2, Object obj1);

        int UpdateByAttributes(Type class1, string s, Object obj, Dictionary<string, object> map);

        int UpdateByAttributes(Type class1, string s, Object obj, Dictionary<string, object> map, string[] vs);

        int UpdateByAttributes(Type class1, Dictionary<string, object> map, string s, string s1);

        int UpdateByAttributes(Type class1, Dictionary<string, object> map, Dictionary<string, object> map1);

        int UpdateByAttributes(Type class1, Dictionary<string, object> map, Dictionary<string, object> map1, string[] vs);

        int UpdateByAttributes(string s, string s1, Object obj, Dictionary<string, object> map);

        int UpdateByAttributes(string s, string s1, Object obj, Dictionary<string, object> map, string[] vs);

        int UpdateByAttributes(string s, Dictionary<string, object> map, Dictionary<string, object> map1, string[] vs);

        int UpdateByAttributes(string s, Dictionary<string, object> map, Dictionary<string, object> map1);

        int UpdateByAttributes(string s, Dictionary<string, object> map, string s1, string s2);

        int UpdateByListAttributes(string s, string s1, Object obj, ArrayList list);

        int DeleteByHql(string s);

        int DeleteByHql(string s, bool flag);

        int DeleteByHql(string s, string s1);

        int DeleteByHql(string s, string s1, bool flag);

        int DeleteByHql(string s, ArrayList list);

        int DeleteByHql(string s, ArrayList list, bool flag);

        void Delete(Object obj);

        void Delete(Object obj, bool flag);

        int Delete(Type class1, ISerializable serializable);

        int Delete(string s, ISerializable serializable);

        int DeleteByName(Type class1, string s);

        int DeleteByName(string s, string s1);

        int DeleteByAttribute(Type class1, string s, Object obj);

        int DeleteByAttribute(string s, string s1, Object obj);

        int DeleteByAttribute(Type class1, string s, Object obj, bool flag);

        int DeleteByAttribute(string s, string s1, Object obj, bool flag);

        int DeleteByAttributes(Type class1, Dictionary<string, object> map);

        int DeleteByAttributes(string s, Dictionary<string, object> map);

        int DeleteByAttributes(Type class1, Dictionary<string, object> map, bool flag);

        int DeleteByAttributes(string s, Dictionary<string, object> map, bool flag);

        int DeleteByAttributes(Type class1, Dictionary<string, object> map, string[] vs);

        int DeleteByAttributes(string s, Dictionary<string, object> map, string[] vs);

        int DeleteByAttributes(Type class1, Type[] atype, string[] vs, Object[] aobj, string[] vs1);

        int DeleteByAttributes(string s, Type[] atype, string[] vs, Object[] aobj, string[] vs1);

        int DeleteByTime(Type class1, DateTime date, DateTime date1);

        int DeleteByTime(string s, DateTime date, DateTime date1);

        int DeleteByTime(Type class1, DateTime date, DateTime date1, int i);

        int DeleteByTime(string s, DateTime date, DateTime date1, int i);

        int DeleteByTime(Type class1, DateTime date);

        int DeleteByTime(string s, DateTime date);

        int DeleteByTime(Type class1, DateTime date, int i);

        int DeleteByTime(string s, DateTime date, int i);

        int DeleteByTime(Type class1, DateTime date, int i, bool flag);

        int DeleteByTime(string s, DateTime date, int i, bool flag);

        int DeleteAll(Type class1);

        int DeleteAll(string s);

        int DeleteAll(Type class1, bool flag);

        int DeleteAll(string s, bool flag);

        void DeleteAll(ICollection collection);

        Object FindByNameWithoutException(Type class1, Object obj);

        Object FindByNameWithoutException(string s, Object obj);

        IList FindByLike(Type class1, string s, Object obj);

        IList FindByLike(string s, string s1, Object obj);

        IList FindByLikeOrderByDesc(Type class1, string s, Object obj, string s1);

        IList FindByLikeOrderByDesc(string s, string s1, Object obj, string s2);

        IList FindByLikeOrderByAsc(Type class1, string s, Object obj, string s1);

        IList FindByLikeOrderByAsc(string s, string s1, Object obj, string s2);

        int UpdateByAttributes(Type class1, Dictionary<string, object> map, string s, Object obj);

        int UpdateByAttributes(string s, Dictionary<string, object> map, string s1, Object obj);

        IList<T> FindByBindingQuery<T>(string s, ArrayList list);

        void Evict(Object obj);

        int ExecuteUpdate(string s);
    }
}
