// ============================================================
// FERRETERÍA JOEL — AUDITORÍA Y BAJA LÓGICA DE PROVEEDORES
// Base de datos: bdproveedores
//
// El microservicio aplica este backfill automáticamente al iniciar.
// Este script permite ejecutarlo manualmente y verificar el resultado.
//
// Ejecutar:
//   docker cp Scripts/05-proveedores-auditoria-mongo.js ferreteria-mongo-proveedores:/tmp/05.js
//   docker exec ferreteria-mongo-proveedores mongosh --file /tmp/05.js
// ============================================================

db = db.getSiblingDB('bdproveedores');
const ahora = new Date();

const resultado = {
  estado: db.proveedores.updateMany(
    { estado: { $exists: false } },
    { $set: { estado: true } }
  ).modifiedCount,
  fechaCreacion: db.proveedores.updateMany(
    { fechaCreacion: { $exists: false } },
    { $set: { fechaCreacion: ahora } }
  ).modifiedCount,
  usuarioCreacion: db.proveedores.updateMany(
    { usuarioCreacion: { $exists: false } },
    { $set: { usuarioCreacion: 'jrios' } }
  ).modifiedCount
};

print('Documentos actualizados: ' + JSON.stringify(resultado));
print('Total proveedores: ' + db.proveedores.countDocuments({}));
printjson(db.proveedores.find(
  {},
  {
    razonSocial: 1,
    estado: 1,
    fechaCreacion: 1,
    usuarioCreacion: 1,
    fechaModificacion: 1,
    fechaBaja: 1
  }
).toArray());
