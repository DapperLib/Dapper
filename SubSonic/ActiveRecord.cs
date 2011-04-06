


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using SubSonic.DataProviders;
using SubSonic.Extensions;
using System.Linq.Expressions;
using SubSonic.Schema;
using System.Collections;
using SubSonic;
using SubSonic.Repository;
using System.ComponentModel;
using System.Data.Common;

namespace SubSonic
{
    
    
    /// <summary>
    /// A class which represents the Posts table in the tempdb Database.
    /// </summary>
    public partial class Post: IActiveRecord
    {
    
        #region Built-in testing
        static TestRepository<Post> _testRepo;
        

        
        static void SetTestRepo(){
            _testRepo = _testRepo ?? new TestRepository<Post>(new SubSonic.tempdbDB());
        }
        public static void ResetTestRepo(){
            _testRepo = null;
            SetTestRepo();
        }
        public static void Setup(List<Post> testlist){
            SetTestRepo();
            foreach (var item in testlist)
            {
                _testRepo._items.Add(item);
            }
        }
        public static void Setup(Post item) {
            SetTestRepo();
            _testRepo._items.Add(item);
        }
        public static void Setup(int testItems) {
            SetTestRepo();
            for(int i=0;i<testItems;i++){
                Post item=new Post();
                _testRepo._items.Add(item);
            }
        }
        
        public bool TestMode = false;


        #endregion

        IRepository<Post> _repo;
        ITable tbl;
        bool _isNew;
        public bool IsNew(){
            return _isNew;
        }
        
        public void SetIsLoaded(bool isLoaded){
            _isLoaded=isLoaded;
            if(isLoaded)
                OnLoaded();
        }
        
        public void SetIsNew(bool isNew){
            _isNew=isNew;
        }
        bool _isLoaded;
        public bool IsLoaded(){
            return _isLoaded;
        }
                
        List<IColumn> _dirtyColumns;
        public bool IsDirty(){
            return _dirtyColumns.Count>0;
        }
        
        public List<IColumn> GetDirtyColumns (){
            return _dirtyColumns;
        }

        SubSonic.tempdbDB _db;
        public Post(string connectionString, string providerName) {

            _db=new SubSonic.tempdbDB(connectionString, providerName);
            Init();            
         }
        void Init(){
            TestMode=this._db.DataProvider.ConnectionString.Equals("test", StringComparison.InvariantCultureIgnoreCase);
            _dirtyColumns=new List<IColumn>();
            if(TestMode){
                Post.SetTestRepo();
                _repo=_testRepo;
            }else{
                _repo = new SubSonicRepository<Post>(_db);
            }
            tbl=_repo.GetTable();
            SetIsNew(true);
            OnCreated();       

        }
        
        public Post(){
             _db=new SubSonic.tempdbDB();
            Init();            
        }
        
       
        partial void OnCreated();
            
        partial void OnLoaded();
        
        partial void OnSaved();
        
        partial void OnChanged();
        
        public IList<IColumn> Columns{
            get{
                return tbl.Columns;
            }
        }

        public Post(Expression<Func<Post, bool>> expression):this() {

            SetIsLoaded(_repo.Load(this,expression));
        }
        
       
        
        internal static IRepository<Post> GetRepo(string connectionString, string providerName){
            SubSonic.tempdbDB db;
            if(String.IsNullOrEmpty(connectionString)){
                db=new SubSonic.tempdbDB();
            }else{
                db=new SubSonic.tempdbDB(connectionString, providerName);
            }
            IRepository<Post> _repo;
            
            if(db.TestMode){
                Post.SetTestRepo();
                _repo=_testRepo;
            }else{
                _repo = new SubSonicRepository<Post>(db);
            }
            return _repo;        
        }       
        
        internal static IRepository<Post> GetRepo(){
            return GetRepo("","");
        }
        
        public static Post SingleOrDefault(Expression<Func<Post, bool>> expression) {
            var repo = GetRepo();
            var results=repo.Find(expression);
            Post single=null;
            if(results.Count() > 0){
                single=results.ToList()[0];
                single.OnLoaded();
                single.SetIsLoaded(true);
                single.SetIsNew(false);
            }

            return single;
        }      
        
        public static Post SingleOrDefault(Expression<Func<Post, bool>> expression,string connectionString, string providerName) {
            var repo = GetRepo(connectionString,providerName);
            var results=repo.Find(expression);
            Post single=null;
            if(results.Count() > 0){
                single=results.ToList()[0];
            }

            return single;


        }
        
        
        public static bool Exists(Expression<Func<Post, bool>> expression,string connectionString, string providerName) {
           
            return All(connectionString,providerName).Any(expression);
        }        
        public static bool Exists(Expression<Func<Post, bool>> expression) {
           
            return All().Any(expression);
        }        

        public static IList<Post> Find(Expression<Func<Post, bool>> expression) {
            
            var repo = GetRepo();
            return repo.Find(expression).ToList();
        }
        
        public static IList<Post> Find(Expression<Func<Post, bool>> expression,string connectionString, string providerName) {

            var repo = GetRepo(connectionString,providerName);
            return repo.Find(expression).ToList();

        }
        public static IQueryable<Post> All(string connectionString, string providerName) {
            return GetRepo(connectionString,providerName).GetAll();
        }
        public static IQueryable<Post> All() {
            return GetRepo().GetAll();
        }
        
        public static PagedList<Post> GetPaged(string sortBy, int pageIndex, int pageSize,string connectionString, string providerName) {
            return GetRepo(connectionString,providerName).GetPaged(sortBy, pageIndex, pageSize);
        }
      
        public static PagedList<Post> GetPaged(string sortBy, int pageIndex, int pageSize) {
            return GetRepo().GetPaged(sortBy, pageIndex, pageSize);
        }

        public static PagedList<Post> GetPaged(int pageIndex, int pageSize,string connectionString, string providerName) {
            return GetRepo(connectionString,providerName).GetPaged(pageIndex, pageSize);
            
        }


        public static PagedList<Post> GetPaged(int pageIndex, int pageSize) {
            return GetRepo().GetPaged(pageIndex, pageSize);
            
        }

        public string KeyName()
        {
            return "Id";
        }

        public object KeyValue()
        {
            return this.Id;
        }
        
        public void SetKeyValue(object value) {
            if (value != null && value!=DBNull.Value) {
                var settable = value.ChangeTypeTo<int>();
                this.GetType().GetProperty(this.KeyName()).SetValue(this, settable, null);
            }
        }
        
        public override string ToString(){
                            return this.Text.ToString();
                    }

        public override bool Equals(object obj){
            if(obj.GetType()==typeof(Post)){
                Post compare=(Post)obj;
                return compare.KeyValue()==this.KeyValue();
            }else{
                return base.Equals(obj);
            }
        }

        
        public override int GetHashCode() {
            return this.Id;
        }
        
        public string DescriptorValue()
        {
                            return this.Text.ToString();
                    }

        public string DescriptorColumn() {
            return "Text";
        }
        public static string GetKeyColumn()
        {
            return "Id";
        }        
        public static string GetDescriptorColumn()
        {
            return "Text";
        }
        
        #region ' Foreign Keys '
        #endregion
        

        int _Id;
        public int Id
        {
            get { return _Id; }
            set
            {
                if(_Id!=value){
                    _Id=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Id");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        string _Text;
        public string Text
        {
            get { return _Text; }
            set
            {
                if(_Text!=value){
                    _Text=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Text");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        DateTime _CreationDate;
        public DateTime CreationDate
        {
            get { return _CreationDate; }
            set
            {
                if(_CreationDate!=value){
                    _CreationDate=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="CreationDate");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        DateTime _LastChangeDate;
        public DateTime LastChangeDate
        {
            get { return _LastChangeDate; }
            set
            {
                if(_LastChangeDate!=value){
                    _LastChangeDate=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="LastChangeDate");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        int? _Counter1;
        public int? Counter1
        {
            get { return _Counter1; }
            set
            {
                if(_Counter1!=value){
                    _Counter1=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Counter1");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        int? _Counter2;
        public int? Counter2
        {
            get { return _Counter2; }
            set
            {
                if(_Counter2!=value){
                    _Counter2=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Counter2");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        int? _Counter3;
        public int? Counter3
        {
            get { return _Counter3; }
            set
            {
                if(_Counter3!=value){
                    _Counter3=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Counter3");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        int? _Counter4;
        public int? Counter4
        {
            get { return _Counter4; }
            set
            {
                if(_Counter4!=value){
                    _Counter4=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Counter4");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        int? _Counter5;
        public int? Counter5
        {
            get { return _Counter5; }
            set
            {
                if(_Counter5!=value){
                    _Counter5=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Counter5");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        int? _Counter6;
        public int? Counter6
        {
            get { return _Counter6; }
            set
            {
                if(_Counter6!=value){
                    _Counter6=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Counter6");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        int? _Counter7;
        public int? Counter7
        {
            get { return _Counter7; }
            set
            {
                if(_Counter7!=value){
                    _Counter7=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Counter7");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        int? _Counter8;
        public int? Counter8
        {
            get { return _Counter8; }
            set
            {
                if(_Counter8!=value){
                    _Counter8=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Counter8");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }

        int? _Counter9;
        public int? Counter9
        {
            get { return _Counter9; }
            set
            {
                if(_Counter9!=value){
                    _Counter9=value;
                    var col=tbl.Columns.SingleOrDefault(x=>x.Name=="Counter9");
                    if(col!=null){
                        if(!_dirtyColumns.Any(x=>x.Name==col.Name) && _isLoaded){
                            _dirtyColumns.Add(col);
                        }
                    }
                    OnChanged();
                }
            }
        }



        public DbCommand GetUpdateCommand() {
            if(TestMode)
                return _db.DataProvider.CreateCommand();
            else
                return this.ToUpdateQuery(_db.Provider).GetCommand().ToDbCommand();
            
        }
        public DbCommand GetInsertCommand() {
 
            if(TestMode)
                return _db.DataProvider.CreateCommand();
            else
                return this.ToInsertQuery(_db.Provider).GetCommand().ToDbCommand();
        }
        
        public DbCommand GetDeleteCommand() {
            if(TestMode)
                return _db.DataProvider.CreateCommand();
            else
                return this.ToDeleteQuery(_db.Provider).GetCommand().ToDbCommand();
        }
       
        
        public void Update(){
            Update(_db.DataProvider);
        }
        
        public void Update(IDataProvider provider){
        
            
            if(this._dirtyColumns.Count>0){
                _repo.Update(this,provider);
                _dirtyColumns.Clear();    
            }
            OnSaved();
       }
 
        public void Add(){
            Add(_db.DataProvider);
        }
        
        
       
        public void Add(IDataProvider provider){

            
            var key=KeyValue();
            if(key==null){
                var newKey=_repo.Add(this,provider);
                this.SetKeyValue(newKey);
            }else{
                _repo.Add(this,provider);
            }
            SetIsNew(false);
            OnSaved();
        }
        
                
        
        public void Save() {
            Save(_db.DataProvider);
        }      
        public void Save(IDataProvider provider) {
            
           
            if (_isNew) {
                Add(provider);
                
            } else {
                Update(provider);
            }
            
        }

        

        public void Delete(IDataProvider provider) {
                   
                 
            _repo.Delete(KeyValue());
            
                    }


        public void Delete() {
            Delete(_db.DataProvider);
        }


        public static void Delete(Expression<Func<Post, bool>> expression) {
            var repo = GetRepo();
            
       
            
            repo.DeleteMany(expression);
            
        }

        

        public void Load(IDataReader rdr) {
            Load(rdr, true);
        }
        public void Load(IDataReader rdr, bool closeReader) {
            if (rdr.Read()) {

                try {
                    rdr.Load(this);
                    SetIsNew(false);
                    SetIsLoaded(true);
                } catch {
                    SetIsLoaded(false);
                    throw;
                }
            }else{
                SetIsLoaded(false);
            }

            if (closeReader)
                rdr.Dispose();
        }
        

    } 
}
