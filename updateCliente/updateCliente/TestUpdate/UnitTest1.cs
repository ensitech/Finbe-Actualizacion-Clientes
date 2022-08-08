using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using updateCliente;


namespace updateCliente.TestUpdate
{
    /// <summary>
    /// Descripción resumida de UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        //public UnitTest1()
        //{
        //    //
        //    // TODO: Agregar aquí la lógica del constructor
        //    updateCliente.UpdateCliente plugin = new updateCliente.UpdateCliente();
        //    plugin.insertInAx(new Guid("DC3A142F-1CF2-E611-A5F1-005056851F55"), CRMLogin.createService(), "contact");//account
        //    /// insertInAx(entity.Id, serviceProxy, entity.LogicalName)
        //    //
        //}

        private TestContext testContextInstance;

        /// <summary>
        ///Obtiene o establece el contexto de las pruebas que proporciona
        ///información y funcionalidad para la ejecución de pruebas actual.
        ///</summary>
        //public TestContext TestContext
        //{
        //    get
        //    {
        //        return testContextInstance;
        //    }
        //    set
        //    {
        //        testContextInstance = value;
        //    }
        //}

        #region Atributos de prueba adicionales
        //
        // Puede usar los siguientes atributos adicionales conforme escribe las pruebas:
        //
        // Use ClassInitialize para ejecutar el código antes de ejecutar la primera prueba en la clase
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup para ejecutar el código una vez ejecutadas todas las pruebas en una clase
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Usar TestInitialize para ejecutar el código antes de ejecutar cada prueba 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup para ejecutar el código una vez ejecutadas todas las pruebas
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestMethod1()
        {
            updateCliente.UpdateCliente plugin = new updateCliente.UpdateCliente();//FEA9C8EC-314D-E711-A8DC-005056855416
            plugin.insertInAx(new Guid("DC3A142F-1CF2-E611-A5F1-005056851F55"), CRMLogin.createService(), "account");
        }
    }
}
