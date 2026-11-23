// Migración de proveedores: esquema relacional antiguo (bdferreteria.proveedor)
// al nuevo microservicio de Proveedores en MongoDB (bdproveedores.proveedores).
//
// Fuente (proyecto anterior): tabla `proveedor` + `empleado` para resolver el
// nombre del responsable (IdEmpleadoResponsable -> Nombre Apellido).
//
// Mapeo aplicado:
//   - Id                 <- IdProveedor
//   - razonSocial        <- RazonSocial
//   - nit                <- generado numérico "9000000<Id>" porque el esquema
//                           antiguo no tenía NIT; ajústalo si corresponde.
//   - contacto           <- NombreContacto
//   - telefono           <- adaptado a 8 dígitos iniciando en 5/6/7 (el antiguo no
//                           cumplía ese formato); ajústalo si corresponde.
//   - nombreResponsable  <- nombre del responsable (desde UI/auditoría). En los
//                           datos semilla antiguos IdEmpleadoResponsable=1 => "Joel Ríos".
//   - Campos no presentes en el nuevo contrato (NombreComercial, CorreoElectronico,
//     Direccion, Estado, fechas) se omiten.
//
// Ejecución (con el contenedor mongo-proveedores en marcha, puerto por defecto 27018):
//   mongosh "mongodb://localhost:27018/bdproveedores" Scripts/migrar_proveedores_antiguo_a_mongo.js
//
// El script es seguro de reejecutar: hace upsert por _id y ajusta el contador.

(function () {
  const dbProveedores = db.getSiblingDB("bdproveedores");
  const proveedores = dbProveedores.getCollection("proveedores");
  const contadores = dbProveedores.getCollection("contadores");

  const datosAntiguos = [
    { _id: 1,  razonSocial: "Distribuidora Ferretera del Norte SA de CV", nit: "90000001", contacto: "Jorge Salinas", telefono: "71000001", nombreResponsable: "Joel Ríos" },
    { _id: 2,  razonSocial: "Herramientas Profesionales del Bajío",        nit: "90000002", contacto: "Rosa Herrera",  telefono: "71000002", nombreResponsable: "Joel Ríos" },
    { _id: 3,  razonSocial: "Suministros Eléctricos MX",                   nit: "90000003", contacto: "Hugo Marrero",  telefono: "71000003", nombreResponsable: "Joel Ríos" },
    { _id: 4,  razonSocial: "Plomería y Tubería Nacional",                 nit: "90000004", contacto: "Sara Duarte",   telefono: "71000004", nombreResponsable: "Joel Ríos" },
    { _id: 5,  razonSocial: "Pinturas y Acabados del Sur",                 nit: "90000005", contacto: "Mario Cano",    telefono: "71000005", nombreResponsable: "Joel Ríos" },
    { _id: 6,  razonSocial: "Fijaciones Técnicas del Centro",              nit: "90000006", contacto: "Nadia Ríos",    telefono: "71000006", nombreResponsable: "Joel Ríos" },
    { _id: 7,  razonSocial: "Seguridad Industrial Integral",               nit: "90000007", contacto: "Raúl Pineda",   telefono: "71000007", nombreResponsable: "Joel Ríos" },
    { _id: 8,  razonSocial: "Adhesivos y Químicos del Golfo",              nit: "90000008", contacto: "Irene Solís",   telefono: "71000008", nombreResponsable: "Joel Ríos" },
    { _id: 9,  razonSocial: "Materiales de Construcción del Pacífico",     nit: "90000009", contacto: "Tomás Lira",    telefono: "71000009", nombreResponsable: "Joel Ríos" },
    { _id: 10, razonSocial: "Importadora de Herramientas Globales",        nit: "90000010", contacto: "Karla Mena",    telefono: "71000010", nombreResponsable: "Joel Ríos" }
  ];

  let upserts = 0;
  datosAntiguos.forEach((p) => {
    const res = proveedores.updateOne(
      { _id: p._id },
      { $set: p },
      { upsert: true }
    );
    if (res.upsertedCount || res.modifiedCount || res.matchedCount) {
      upserts += 1;
    }
  });

  const maxId = datosAntiguos.reduce((m, p) => Math.max(m, p._id), 0);
  contadores.updateOne(
    { _id: "proveedores" },
    { $max: { secuencia: maxId } },
    { upsert: true }
  );

  print(`Migración completada. Documentos procesados: ${upserts}. Contador ajustado a >= ${maxId}.`);
})();
