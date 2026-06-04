class IntentRouterService {
  classify(text) {
    const t = String(text || '').toLowerCase();
    const rules = [ ['memoria', /memoria|recordar/], ['codigo', /c[oó]digo|code|buscar.*funci[oó]n/], ['vault', /vault|b[oó]veda|nota/], ['nas', /nas|red compartida|samba/], ['auditoria', /auditor|hash-chain|log/], ['sistema', /sistema|ip|ntp|doctor|diagn[oó]stico/], ['mqtt', /mqtt|broker|topic/], ['wol', /wake|wol|magic packet/], ['docker', /docker|contenedor|deploy/], ['rbac', /rbac|rol|permiso|usuario/], ['seguridad', /secreto|token|zeroleak|cifrar|totp/], ['dependencia', /dependencia|deps|instalar|missing_dependency/] ];
    return (rules.find(([, r]) => r.test(t)) || ['desconocido'])[0];
  }
}
module.exports = { IntentRouterService };
