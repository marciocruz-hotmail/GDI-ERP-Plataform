using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace GdiPlataform.Lib
{
    public class YesObjects
    {
        public static T Clonar<T>(T objOriginal)
        {
            //Cria nova referência
            object objClonado = Activator.CreateInstance<T>();
            //Pega as propriedades
            PropertyDescriptorCollection propriedades = TypeDescriptor.GetProperties(objOriginal);
            //Seta as propriedades
            for (int i = 0; i < propriedades.Count; i++)
            {
                propriedades[i].SetValue(objClonado, propriedades[i].GetValue(objOriginal));
            }
            //Retorna objeto clonado
            return (T)objClonado;
        }
    }
}