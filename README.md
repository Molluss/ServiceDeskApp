Service Desk App
Description
Service Desk App est une application conçue pour simplifier les tâches des techniciens de support IT. Elle permet de :

Gérer des templates pour des réponses rapides aux tickets.
Automatiser partiellement les tâches liées aux tickets (copier/coller de modèles).
Personnaliser les paramètres, comme le mode sombre et la gestion des sources de fichiers (HTTP, OneDrive, réseau local).
Fonctionnalités principales
Gestion des templates :

Affichage de l'arborescence des templates.
Double-cliquez pour copier un modèle dans le presse-papiers.
Synchronisation des templates depuis une source distante.
Support des tickets :

Génération rapide de messages liés aux tickets en renseignant le numéro et le sujet.
Utilisation de modèles prédéfinis personnalisables.
Mode sombre et clair :

Commutation via les paramètres.
Paramètres configurables :

Gestion des emplacements des templates (HTTP, OneDrive, réseau local).
Personnalisation de la signature utilisée dans les templates.
Installation
Pré-requis :

Windows 10 ou supérieur.
.NET Framework 4.8 ou .NET 6.0+ installé (selon la version utilisée).
WebView2 Runtime pour les fonctionnalités basées sur le navigateur.
Téléchargement :

Téléchargez le fichier ZIP ou l'installateur depuis Releases.
Installation :

Si vous utilisez un installateur : lancez le fichier .exe ou .msi.
Sinon, extrayez les fichiers du ZIP dans un dossier.
Exécution :

Double-cliquez sur ServiceDeskApp.exe pour lancer l'application.
Utilisation
Gérer les templates :

Naviguez dans l'arborescence des templates à gauche.
Double-cliquez sur un modèle pour le copier dans le presse-papiers.
Générer un message pour un ticket :

Entrez le numéro et le sujet du ticket dans les champs à droite.
Cliquez sur Generate Message pour copier le message généré.
Modifier les paramètres :

Cliquez sur Settings dans le menu ou le bouton dédié.
Configurez le mode sombre, la source des templates et les emplacements locaux.
Configuration avancée
Personnalisation de la signature :

Allez dans les paramètres.
Ajoutez ou modifiez la signature pour vos templates.
Modification des emplacements de sources :

Configurez les sources depuis les paramètres en choisissant :
HTTP.
Réseau local.
OneDrive.
Contributeurs
Lucas (Lead Developer)
Contributions supplémentaires : [Nom du contributeur]
Dépannage
Si vous rencontrez des problèmes :

Assurez-vous que tous les fichiers requis (templates, DLL, etc.) sont présents.
Consultez le fichier log.txt pour plus de détails.
Vérifiez que WebView2 Runtime est installé.
En cas de problème persistant, ouvrez une issue sur GitHub Issues.
Licence
Ce projet est sous licence MIT. Voir LICENSE pour plus de détails.
