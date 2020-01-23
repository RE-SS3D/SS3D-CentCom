create table Users
(
  Id           int auto_increment,
  Email        varchar(128) not null,
  PasswordHash blob         not null,
  PasswordSalt blob         not null,
  PRIMARY KEY (Id),
  constraint users_email_uindex
    unique (Email),
  constraint users_id_uindex
    unique (Id)
);

create table Characters
(
  Id     int auto_increment,
  UserId int          not null,
  Name   varchar(512) not null,
  PRIMARY KEY (Id),
  constraint characters_Id_uindex
    unique (Id),
  constraint characters_users_Id_fk
    foreign key (UserId) references users (Id)
      on update cascade on delete cascade
);