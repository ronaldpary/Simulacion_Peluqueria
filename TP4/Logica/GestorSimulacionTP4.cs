﻿using System;
using System.Collections.Generic;
using System.Linq;
using TP4.Presentacion;
using static TP4.Logica.Cliente;

namespace TP4.Logica
{
    public class GestorSimulacionTP4
    {
        // Creamos los empleados y los peluqueros
        public Empleado aprendiz = new Empleado();
        public Empleado veteranoA = new Empleado();
        public Empleado veteranoB = new Empleado();

        public Peluqueria peluqueroAprendiz;
        public Peluqueria peluqueroVeteA;
        public Peluqueria peluqueroVeteB;

        private USPeluqueria interfaz;
        private Eventos eventos;
        private Generadores generador;

        public Random rndLlegada = new Random();
        public Random rndAtencion = new Random();
        public Random rndPeluquero = new Random();

        //Iniciamos los parametros por defecto
        public double llegadaA = 2;
        public double llegadaB = 12;
        public double aprendizA = 20;
        public double aprendizB = 30;
        public double veteranoAA = 11;
        public double veteranoAB = 13;
        public double veteranoBA = 12;
        public double veteranoBB = 18;

        public double h = 0.1;
        public double TAprendiz = 180;
        public double TVeterano = 130;

        public double probabilidadAp = 0.15;
        public double probabilidadVA = 0.45;
        public double probabilidadVB = 0.40;

        //Variables para los puntos pedidos, acumuladores y contadores
        private int contadorDias;
        private double acumuladorRecaudacion;
        private double promedioRecaudacion;
        private int contadorClientes;
        private int contadorSillasAnterior;

        public bool fin = false;
        public bool findia = false;
        public double numeroDia = 0;

        //Variable para cada fila x
        public Fila fila;

        //Lista de clientes en el sistema
        public List<Cliente> enElSistema;

        public int cantFilasMostradas;

        public GestorSimulacionTP4 (USPeluqueria interfaz) 
        {
            this.interfaz = interfaz;
            this.fila = new Fila();
            this.enElSistema = new List<Cliente>();
            this.peluqueroAprendiz = new Peluqueria(aprendiz);
            this.peluqueroVeteA = new Peluqueria(veteranoA);
            this.peluqueroVeteB = new Peluqueria(veteranoB);
            generador = new Generadores();

        }

        //Metodo que se llama desde la interfaz
        public void iniciarSimulacion(int dias, Parametros parametros, int desde, int hasta)
        {
            eventos = new Eventos(fila, this);
            this.llegadaA = parametros.llegadaA;
            this.llegadaB = parametros.llegadaB;
            this.aprendizA = parametros.aprendizA;
            this.aprendizB = parametros.aprendizB;
            this.veteranoAA = parametros.veteranoAA;
            this.veteranoAB = parametros.veteranoAB;
            this.veteranoBA = parametros.veteranoBA;
            this.veteranoBB = parametros.veteranoBB;
            this.probabilidadAp = parametros.probabilidadAprendiz;
            this.probabilidadVA = parametros.probabilidadVeteranoA;
            this.probabilidadVB = parametros.probabilidadVeteranoB;
            this.h = parametros.h;
            this.TAprendiz = parametros.TAprendiz;
            this.TVeterano = parametros.TVeterano;
            //Metodo para iniciar la simulacion
            comenzarSimulacion(dias, desde, hasta);
        }

        private void comenzarSimulacion(int dias, int desde, int hasta)
        {
            generarTiempoProximaLlegada();
            actualizarEstados();
            actualizarColas();

            //double numeroDia = 0;
            double numeroSimulacion = 0;
            string nombreEvento = "Inicio";

            //Mostramos la fila inicio
            interfaz.mostrarFila(fila, enElSistema, numeroDia, numeroSimulacion, nombreEvento);

            //Para cada dia recorremos
            for (int i = 0; i < dias; i++)
            {
                fin = findia = false;
                numeroDia = i + 1;

                //Mientras haya tiempo para atender a un cliente
                while (fin == false)
                {

                    if (fila.proxima_llegada > 480)
                    {
                        fila.proxima_llegada = -1;
                    }

                    double siguienteTiempo = definirSiguienteTiempo(fila);
                    double relojAnterior = Double.Parse(fila.Reloj.ToString("F2"));
                    fila.Reloj = Double.Parse(siguienteTiempo.ToString("F2"));
                    numeroSimulacion = numeroSimulacion + 1;

                    //fila.fin_dia = (Double.Parse(fila.fin_dia.ToString("F2")) - (fila.Reloj - relojAnterior));
                    fila.fin_dia = 480;

                    // evento fin dia
                    if ((fila.proxima_llegada == -1 && fila.Reloj > 480 && findia == false) || (fila.proxima_llegada == -1 && fila.Reloj < 480 && fila.fin_atencion_aprendiz != -1 && fila.fin_atencion_veteA != -1 && fila.fin_Atencion_veteB != -1 && findia == false))
                    {
                        findia = true;
                        numeroSimulacion = numeroSimulacion + 1;
                        nombreEvento = "Fin dia" + "(" + numeroDia.ToString() + ")";

                        if (numeroSimulacion >= desde && numeroSimulacion <= hasta)
                        {
                            double findiaG = fila.fin_dia;
                            double relojG = fila.Reloj;
                            fila.fin_dia = -1;
                            fila.Reloj = 480;
                            interfaz.mostrarFila(fila, enElSistema, numeroDia, numeroSimulacion, nombreEvento);
                            cantFilasMostradas++;
                            fila.fin_dia = -1;
                            fila.Reloj = relojG;
                        }
                    }

                    // EVENTO REFRIGERIO
                    for (int m = 0; m < enElSistema.Count; m++)
                    {
                        if (enElSistema[m].hora_inicio_espera != 0)
                        {
                            if ((fila.Reloj - enElSistema[m].hora_inicio_espera) > 30 && enElSistema[m].estado != 2 && enElSistema[m].estado != 4 && enElSistema[m].estado != 6 && enElSistema[m].tiene_refri != 1 && enElSistema[m].estado != 7)
                            {
                                numeroSimulacion = numeroSimulacion + 1;
                                nombreEvento = "Llegada refrigerio" + "(" + enElSistema[m].numero.ToString() + ")";

                                fila.Reloj = enElSistema[m].hora_refrigerio;
                                enElSistema[m].tiene_refri = 1;

                                if (fila.Reloj > 480)
                                {
                                    fila.fin_dia = -1;
                                }

                                if (numeroSimulacion >= desde && numeroSimulacion <= hasta)
                                {
                                    interfaz.mostrarFila(fila, enElSistema, numeroDia, numeroSimulacion, nombreEvento);
                                }

                                fila.Reloj = Double.Parse(siguienteTiempo.ToString("F2"));
                            }
                            
                        }

                    }

                    if (Math.Truncate(siguienteTiempo * 10000) / 10000 == Math.Truncate(fila.proxima_llegada * 10000) / 10000)
                    {
                        // Ver si es necesario tener un contador para los clientes que ingresen al sistema
                        Cliente clienteCreado = eventos.proximaLlegada();
                        nombreEvento = "Llegada de cliente " + "(" + clienteCreado.numero.ToString() + ")";
                        //fila.limpiarColumnasTiempoAtencion();

                    }
                    //else if (siguienteTiempo == 480)
                    //{
                    //    eventos.finDia();
                    //    nombreEvento = "Fin dia" + "(" + numeroDia.ToString() + ")";
                    //}
                    else if (Math.Truncate(siguienteTiempo * 10000) / 10000 == Math.Truncate(fila.fin_atencion_aprendiz * 10000) / 10000)
                    {
                        for (int j = 0; j < enElSistema.Count; j++)
                        {
                            if (enElSistema[j].estado == (double)Estado.siendo_atendidoAp)
                            {

                                eventos.finAtencionAprendiz(enElSistema[j]);
                                nombreEvento = "Fin atencion Ap" + "(" + enElSistema[j].numero.ToString() + ")";

                                break;
                            }
                        }
                    }
                    else if (Math.Truncate(siguienteTiempo * 10000) / 10000 == Math.Truncate(fila.fin_atencion_veteA * 10000) / 10000)
                    {

                        for (int j = 0; j < enElSistema.Count; j++)
                        {
                            if (enElSistema[j].estado == (double)Estado.siendo_atendidoA)
                            {

                                eventos.finAtencionVeteA(enElSistema[j]);
                                nombreEvento = "Fin atencion VA" + "(" + enElSistema[j].numero.ToString() + ")";

                                break;
                            }
                        }
                    }
                    else if (Math.Truncate(siguienteTiempo * 10000) / 10000 == Math.Truncate(fila.fin_Atencion_veteB * 10000) / 10000)
                    {
                        for (int j = 0; j < enElSistema.Count; j++)
                        {
                            if (enElSistema[j].estado == (double)Estado.siendo_atendidoB)
                            {

                                eventos.finAtencionVeteB(enElSistema[j]);
                                nombreEvento = "Fin atencion VB" + "(" + enElSistema[j].numero.ToString() + ")";

                                break;
                            }
                        }
                    }

                    actualizarColas();
                    actualizarEstados();

                    if (contadorSillasAnterior >= eventos.cantidadMaximaSillas())
                    {
                        fila.clientes_maximos = contadorSillasAnterior;
                    }
                    else
                    {
                        contadorSillasAnterior = eventos.cantidadMaximaSillas();
                        fila.clientes_maximos = eventos.cantidadMaximaSillas();
                    }


                    if (numeroSimulacion >= desde && numeroSimulacion <= hasta)
                    {
                        interfaz.mostrarFila(fila, enElSistema, numeroDia, numeroSimulacion, nombreEvento);
                        cantFilasMostradas++;
                    }

                    //Eliminamos los clientes que hayan sido atendidos
                    //eliminarClientesAtendidos();      // COMENTAR ESTE FILA SI LO DEJAMOS CON EL FALLO DE LAS COLUMNAS


                    //fin dia
                    if ((enElSistema.All(Cliente => Cliente.estado == 7) && fila.Reloj >= 480) || (enElSistema.All(Cliente => Cliente.estado == 7) && fila.Reloj < 480 && fila.fin_atencion_aprendiz != -1 && fila.fin_atencion_veteA != -1 && fila.fin_Atencion_veteB != -1))
                    {
                        eventos.finDia();
                        if (numeroDia < dias)
                        {
                            fila.Reloj = 0;
                            generarTiempoProximaLlegada();
                        }
                        fin = true;

                        if (numeroSimulacion >= desde && numeroSimulacion <= hasta && numeroDia < dias)
                        {
                            numeroDia = numeroDia + 1;
                            numeroSimulacion = numeroSimulacion + 1;
                            nombreEvento = "Inicio";
                           // generarTiempoProximaLlegada();
                            fila.limpiarColumnasFinDia();
                            interfaz.mostrarFila(fila, enElSistema, numeroDia, numeroSimulacion, nombreEvento);
                        }

                    }

                    //if ((enElSistema.Count == 0 && fila.Reloj >= 480) || (enElSistema.Count == 0 && fila.Reloj < 480 && fila.fin_atencion_aprendiz != -1 && fila.fin_atencion_veteA != -1 && fila.fin_Atencion_veteB != -1))
                    //{
                    //    eventos.finDia();
                    //    if (numeroDia < dias)
                    //    {
                    //        fila.Reloj = 0;
                    //        generarTiempoProximaLlegada();
                    //    }
                    //    fin = true;

                    //    if (numeroSimulacion >= desde && numeroSimulacion <= hasta && numeroDia < dias)
                    //    {
                    //        numeroDia = numeroDia + 1;
                    //        numeroSimulacion = numeroSimulacion + 1;
                    //        nombreEvento = "Inicio";
                    //        fila.limpiarColumnasFinDia();
                    //        interfaz.mostrarFila(fila, enElSistema, numeroDia, numeroSimulacion, nombreEvento);
                    //    }

                    //}

                }

            }

            //Mostramos la ultima fila
            if (hasta < numeroSimulacion) {
                interfaz.mostrarFila(fila, enElSistema, numeroDia, numeroSimulacion, nombreEvento);
            }
            

        }

        private double definirSiguienteTiempo(Fila fila)
        {
            List<double> listaEventos = new Double[] {fila.proxima_llegada, fila.fin_atencion_aprendiz, fila.fin_atencion_veteA, fila.fin_Atencion_veteB}.ToList();
            listaEventos.RemoveAll(x => x == -1);
            double minimo;
            if (listaEventos.Count != 0)
            {
                minimo = listaEventos.Min();
            }
            else {
                generarTiempoProximaLlegada();
                minimo = fila.proxima_llegada;
            }
            return minimo;
        }

        public void eliminarClientesAtendidos()
        {
            enElSistema.RemoveAll(clienteAtendido);
        }

        private bool clienteAtendido(Cliente obj)
        {
            return obj.estado == (double)Estado.destruido;
        }

        private void actualizarColas()
        {
            fila.cola_aprendiz = peluqueroAprendiz.cola.Count;
            fila.cola_veteA = peluqueroVeteA.cola.Count;
            fila.cola_veteB = peluqueroVeteB.cola.Count;
        }

        private void actualizarEstados()
        {
            fila.estado_aprendiz = peluqueroAprendiz.empleado.estado;
            fila.estado_veteA = peluqueroVeteA.empleado.estado;
            fila.estado_veteB = peluqueroVeteB.empleado.estado;
        }

        public void generarTiempoProximaLlegada()
        {
            fila.rnd_llegada = Double.Parse(rndLlegada.NextDouble().ToString("F2"));
            fila.tiempo_entre_llegada = generador.GeneradorUniforme(llegadaA, llegadaB, fila.rnd_llegada);
            fila.proxima_llegada = fila.Reloj + fila.tiempo_entre_llegada;
        }

        public void generarTiempoAtencionVeteB()
        {
            //double colaVeteB = fila.cola_veteB;
            double colaVeteB = peluqueroVeteB.cola.Count;
            Euler eu = new Euler(this.h, 0, 0, new FinAtencionVeteranoB(), null, colaVeteB, this.TVeterano);
            double t = eu.calcularEuler();

            fila.rnd_atencion = Double.Parse(rndAtencion.NextDouble().ToString("F2"));
            //fila.tiempo_atencion = generador.GeneradorUniforme(veteranoBA, veteranoBB, fila.rnd_atencion);
            fila.tiempo_atencion = t;
            fila.fin_Atencion_veteB = fila.Reloj + fila.tiempo_atencion;
        }

        public void generarTiempoAtencionVeteA()
        {
            //double colaVeteA = fila.cola_veteA;
            double colaVeteA = peluqueroVeteA.cola.Count;
            Euler eu = new Euler(this.h, 0, 0, new FinAtencionVeteranoA(), null, colaVeteA, this.TVeterano);
            double t = eu.calcularEuler();

            fila.rnd_atencion = Double.Parse(rndAtencion.NextDouble().ToString("F2"));
            //fila.tiempo_atencion = generador.GeneradorUniforme(veteranoAA, veteranoAB, fila.rnd_atencion);
            fila.tiempo_atencion = t;
            fila.fin_atencion_veteA = fila.Reloj + fila.tiempo_atencion;
        }

        public void generarTiempoAtencionAP()
        {
            //double colaAprendiz = fila.cola_aprendiz;
            double colaAprendiz = peluqueroAprendiz.cola.Count;
            Euler eu = new Euler(this.h, 0, 0, new FinAtencionAprendiz(), null, colaAprendiz, this.TAprendiz);
            double t = eu.calcularEuler();

            fila.rnd_atencion = Double.Parse(rndAtencion.NextDouble().ToString("F2"));
            //fila.tiempo_atencion = generador.GeneradorUniforme(aprendizA, aprendizB, fila.rnd_atencion);
            fila.tiempo_atencion = t;
            fila.fin_atencion_aprendiz = fila.Reloj + fila.tiempo_atencion;
        }

        internal double tomarColaAp()
        {
            return fila.cola_aprendiz;
        }

        internal double tomarColaVA()
        {
            return fila.cola_veteA;
        }

        internal double tomarColaVB()
        {
            return fila.cola_veteB;
        }
    }
}
